// js/transactions.js
// Handles UI updates for table and cards, CRUD for transactions & categories

let currentFilter = { month: 'all', year: 'all', type: 'all' };

function updateDashboard() {
    const allTrans = getTransactions();

    // Apply filters
    const filteredTrans = allTrans.filter(t => {
        const d = new Date(t.date);
        const matchMonth = currentFilter.month === 'all' || d.getMonth() === parseInt(currentFilter.month);
        const matchYear = currentFilter.year === 'all' || d.getFullYear() === parseInt(currentFilter.year);
        const matchType = currentFilter.type === 'all' || t.type === currentFilter.type;
        return matchMonth && matchYear && matchType;
    });

    // Update Table
    renderTable(filteredTrans);

    // Update Cards
    updateSummaryCards(filteredTrans);

    // Update Charts (from charts.js)
    if (typeof updateCharts === 'function') {
        updateCharts(filteredTrans);
    }
}

function renderTable(transactions) {
    const tbody = document.getElementById('transactionsTableBody');
    tbody.innerHTML = '';

    // Sort by date descending
    transactions.sort((a, b) => new Date(b.date) - new Date(a.date)).forEach(t => {
        const isIncome = t.type === 'Income';
        const colorClass = isIncome ? 'text-success' : 'text-danger';
        const badgeClass = isIncome ? 'bg-success' : 'bg-danger';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${t.date}</td>
            <td><span class="badge ${badgeClass}">${t.type}</span></td>
            <td>${t.category}</td>
            <td>${t.description}</td>
            <td class="text-end fw-bold ${colorClass}">${formatCurrency(t.amount)}</td>
            <td class="text-center">
                <i class="fas fa-edit text-primary action-icon" onclick="editTransaction(${t.id})"></i>
                <i class="fas fa-trash text-danger action-icon" onclick="deleteTransaction(${t.id})"></i>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function updateSummaryCards(transactions) {
    let income = 0;
    let expense = 0;

    transactions.forEach(t => {
        if (t.type === 'Income') income += parseFloat(t.amount);
        else expense += parseFloat(t.amount);
    });

    const net = income - expense;

    document.getElementById('totalIncomeDisplay').textContent = formatCurrency(income);
    document.getElementById('totalExpenseDisplay').textContent = formatCurrency(expense);
    document.getElementById('netProfitDisplay').textContent = formatCurrency(net);

    const profitEl = document.getElementById('netProfitDisplay');
    profitEl.className = 'mb-0 ' + (net >= 0 ? 'text-success' : 'text-danger');
}

// Add / Edit Transaction
document.getElementById('transactionForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const id = document.getElementById('transId').value;
    const date = document.getElementById('transDate').value;
    const type = document.getElementById('transType').value;
    const category = document.getElementById('transCategory').value;
    const desc = document.getElementById('transDesc').value;
    const amount = parseFloat(document.getElementById('transAmount').value);

    let transactions = getTransactions();

    if (id) {
        // Edit existing
        const index = transactions.findIndex(t => t.id == id);
        if (index > -1) {
            transactions[index] = { id: parseInt(id), date, type, category, description: desc, amount };
        }
    } else {
        // Add new
        const newId = transactions.length > 0 ? Math.max(...transactions.map(t => t.id)) + 1 : 1;
        transactions.push({ id: newId, date, type, category, description: desc, amount });
    }

    saveTransactions(transactions);

    // Close modal & refresh
    bootstrap.Modal.getInstance(document.getElementById('transactionModal')).hide();
    updateDashboard();
});

function prepareAddForm() {
    document.getElementById('transactionForm').reset();
    document.getElementById('transId').value = '';
    document.getElementById('transactionModalTitle').textContent = 'Add Transaction';

    // Set default date to today
    document.getElementById('transDate').value = new Date().toISOString().split('T')[0];
    updateCategoryDropdown();
}

function editTransaction(id) {
    const transactions = getTransactions();
    const t = transactions.find(t => t.id === id);
    if (t) {
        document.getElementById('transId').value = t.id;
        document.getElementById('transDate').value = t.date;
        document.getElementById('transType').value = t.type;
        updateCategoryDropdown();
        document.getElementById('transCategory').value = t.category;
        document.getElementById('transDesc').value = t.description;
        document.getElementById('transAmount').value = t.amount;

        document.getElementById('transactionModalTitle').textContent = 'Edit Transaction';
        new bootstrap.Modal(document.getElementById('transactionModal')).show();
    }
}

function deleteTransaction(id) {
    if (confirm('Are you sure you want to delete this transaction?')) {
        let transactions = getTransactions();
        transactions = transactions.filter(t => t.id !== id);
        saveTransactions(transactions);
        updateDashboard();
    }
}

// Category Management CRUD
function updateCategoryDropdown() {
    const type = document.getElementById('transType').value; // Income or Expense
    const categories = getCategories()[type] || [];

    const select = document.getElementById('transCategory');
    select.innerHTML = '';
    categories.forEach(cat => {
        const opt = document.createElement('option');
        opt.value = cat;
        opt.textContent = cat;
        select.appendChild(opt);
    });
}

function renderCategoryLists() {
    const cats = getCategories();

    const renderList = (type, targetId) => {
        const ul = document.getElementById(targetId);
        ul.innerHTML = '';
        cats[type].forEach((cat, index) => {
            const li = document.createElement('li');
            li.className = 'list-group-item';
            li.innerHTML = `
                ${cat}
                <button class="btn btn-sm btn-outline-danger" onclick="deleteCategory('${type}', ${index})">
                    <i class="fas fa-trash"></i>
                </button>
            `;
            ul.appendChild(li);
        });
    }

    renderList('Income', 'incomeCatList');
    renderList('Expense', 'expenseCatList');
}

document.getElementById('addCategoryForm').addEventListener('submit', function (e) {
    e.preventDefault();
    const type = document.getElementById('newCatType').value;
    const name = document.getElementById('newCatName').value.trim();

    if (name) {
        const cats = getCategories();
        if (!cats[type].includes(name)) {
            cats[type].push(name);
            saveCategories(cats);
            renderCategoryLists();
            updateCategoryDropdown();
            document.getElementById('newCatName').value = '';
        } else {
            alert('Category already exists!');
        }
    }
});

function deleteCategory(type, index) {
    const cats = getCategories();
    cats[type].splice(index, 1);
    saveCategories(cats);
    renderCategoryLists();
    updateCategoryDropdown();
}

// Export PDF functionality
document.getElementById('exportBtn').addEventListener('click', function () {
    const dashboard = document.getElementById('dashboardContent');
    const btn = this;

    // Visual feedback
    btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Exporting...';
    btn.disabled = true;

    // Use html2canvas to capture the dashboard
    html2canvas(dashboard, { scale: 2 }).then(canvas => {
        const imgData = canvas.toDataURL('image/png');
        const pdf = new window.jspdf.jsPDF('p', 'mm', 'a4');

        const pdfWidth = pdf.internal.pageSize.getWidth();
        const pdfHeight = (canvas.height * pdfWidth) / canvas.width;

        pdf.text("Farm Wallet Dashboard Report", 14, 15);
        pdf.addImage(imgData, 'PNG', 0, 20, pdfWidth, pdfHeight);
        pdf.save(`Farm_Wallet_Report_${new Date().toISOString().split('T')[0]}.pdf`);

        // Restore button
        btn.innerHTML = '<i class="fas fa-file-pdf text-danger me-1"></i> Export Report';
        btn.disabled = false;
    });
});
