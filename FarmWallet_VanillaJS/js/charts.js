// js/charts.js
// Handles Chart.js initialization and updates

let pieChartInstance = null;
let lineChartInstance = null;

function updateCharts(transactions) {
    updatePieChart(transactions);
    updateLineChart(transactions);
}

function updatePieChart(transactions) {
    const expenses = transactions.filter(t => t.type === 'Expense');

    // Group by category
    const catMap = {};
    expenses.forEach(t => {
        catMap[t.category] = (catMap[t.category] || 0) + parseFloat(t.amount);
    });

    const labels = Object.keys(catMap);
    const data = Object.values(catMap);

    const ctx = document.getElementById('expensePieChart').getContext('2d');

    if (pieChartInstance) {
        pieChartInstance.destroy();
    }

    if (data.length === 0) {
        // Draw empty message or reset
        return;
    }

    pieChartInstance = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: [
                    '#e53935', // Red
                    '#fb8c00', // Orange
                    '#fdd835', // Yellow
                    '#43a047', // Green
                    '#1e88e5', // Blue
                    '#8e24aa'  // Purple
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right' }
            }
        }
    });
}

function updateLineChart(transactions) {
    // Group by month-year for trend
    // Sorting transactions to find date range
    const sorted = [...transactions].sort((a, b) => new Date(a.date) - new Date(b.date));

    if (sorted.length === 0) {
        if (lineChartInstance) lineChartInstance.destroy();
        return;
    }

    const labelsSet = new Set();
    const incomeMap = {};
    const expenseMap = {};

    sorted.forEach(t => {
        const d = new Date(t.date);
        const monthYear = d.toLocaleString('en-US', { month: 'short', year: 'numeric' });
        labelsSet.add(monthYear);

        if (t.type === 'Income') incomeMap[monthYear] = (incomeMap[monthYear] || 0) + parseFloat(t.amount);
        else expenseMap[monthYear] = (expenseMap[monthYear] || 0) + parseFloat(t.amount);
    });

    const labels = Array.from(labelsSet);
    const incomeData = labels.map(l => incomeMap[l] || 0);
    const expenseData = labels.map(l => expenseMap[l] || 0);

    const ctx = document.getElementById('trendLineChart').getContext('2d');

    if (lineChartInstance) {
        lineChartInstance.destroy();
    }

    lineChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Income (₹)',
                    data: incomeData,
                    borderColor: '#43a047',
                    backgroundColor: 'rgba(67, 160, 71, 0.2)',
                    fill: true,
                    tension: 0.3
                },
                {
                    label: 'Expense (₹)',
                    data: expenseData,
                    borderColor: '#e53935',
                    backgroundColor: 'rgba(229, 57, 53, 0.2)',
                    fill: true,
                    tension: 0.3
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}
