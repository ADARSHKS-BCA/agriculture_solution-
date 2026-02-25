// js/filter.js
// Handles Filtering logic for Month and Year

function populateYearDropdown() {
    const transactions = getTransactions();
    const yearSelect = document.getElementById('filterYear');

    // Default fallback
    const currentYear = new Date().getFullYear();
    const years = new Set([currentYear]);

    transactions.forEach(t => {
        years.add(new Date(t.date).getFullYear());
    });

    // Sort descending
    const sortedYears = Array.from(years).sort((a, b) => b - a);

    // Clear except 'all'
    yearSelect.innerHTML = '<option value="all">All Years</option>';

    sortedYears.forEach(year => {
        const opt = document.createElement('option');
        opt.value = year;
        opt.textContent = year;
        yearSelect.appendChild(opt);
    });
}

document.getElementById('applyFilterBtn').addEventListener('click', function () {
    const m = document.getElementById('filterMonth').value;
    const y = document.getElementById('filterYear').value;

    currentFilter.month = m;
    currentFilter.year = y;
    currentFilter.type = 'all'; // Reset type filter on date change

    // Reset active classes on cards
    document.querySelectorAll('.summary-card').forEach(c => c.classList.remove('active-filter'));

    // Check if filtering is active for label
    const label = document.getElementById('tableFilterLabel');
    if (m !== 'all' || y !== 'all') {
        label.style.display = 'inline-block';
        label.textContent = `Filtered: ${m !== 'all' ? document.getElementById('filterMonth').options[document.getElementById('filterMonth').selectedIndex].text : ''} ${y !== 'all' ? y : ''}`;
    } else {
        label.style.display = 'none';
    }

    updateDashboard();
});

// Clickable Summary Cards Logic
document.getElementById('cardIncome').addEventListener('click', () => setTypeFilter('Income', 'cardIncome'));
document.getElementById('cardExpense').addEventListener('click', () => setTypeFilter('Expense', 'cardExpense'));
document.getElementById('cardNetProfit').addEventListener('click', () => setTypeFilter('all', 'cardNetProfit'));

function setTypeFilter(type, activeCardId) {
    currentFilter.type = type;

    // Manage active visual state
    document.querySelectorAll('.summary-card').forEach(c => c.classList.remove('active-filter'));
    document.getElementById(activeCardId).classList.add('active-filter');

    // Update Badge
    const label = document.getElementById('tableFilterLabel');
    if (type !== 'all') {
        label.style.display = 'inline-block';
        label.className = `badge ${type === 'Income' ? 'bg-success' : 'bg-danger'} ms-2`;
        label.textContent = `Showing Only ${type}`;
    } else {
        label.style.display = 'none';
    }

    updateDashboard();
}
