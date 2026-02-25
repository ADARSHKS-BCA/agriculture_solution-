// js/data.js
// Handles Data Initialization and LocalStorage Operations

const STORAGE_KEY = 'farmWalletTransactions';
const CAT_STORAGE_KEY = 'farmWalletCategories';

// Default mock data if empty
const defaultTransactions = [
    { id: 1, date: '2023-10-15', type: 'Income', category: 'Crop Sales', amount: 45000, description: 'Sold Wheat' },
    { id: 2, date: '2023-10-18', type: 'Expense', category: 'Seeds', amount: 12000, description: 'Wheat Seeds' },
    { id: 3, date: '2023-11-05', type: 'Expense', category: 'Fertilizer', amount: 8000, description: 'Urea' },
    { id: 4, date: '2023-11-20', type: 'Income', category: 'Dairy Sales', amount: 15000, description: 'Monthly milk' },
    { id: 5, date: '2023-12-10', type: 'Expense', category: 'Labor', amount: 6000, description: 'Farm hands' }
];

const defaultCategories = {
    Income: ['Crop Sales', 'Dairy Sales', 'Government Subsidy', 'Other Sales'],
    Expense: ['Seeds', 'Fertilizer', 'Labor', 'Equipment', 'Transport', 'Others']
};

function initData() {
    if (!localStorage.getItem(STORAGE_KEY)) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(defaultTransactions));
    }
    if (!localStorage.getItem(CAT_STORAGE_KEY)) {
        localStorage.setItem(CAT_STORAGE_KEY, JSON.stringify(defaultCategories));
    }
}

function getTransactions() {
    return JSON.parse(localStorage.getItem(STORAGE_KEY)) || [];
}

function saveTransactions(transactions) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(transactions));
}

function getCategories() {
    return JSON.parse(localStorage.getItem(CAT_STORAGE_KEY)) || { Income: [], Expense: [] };
}

function saveCategories(categories) {
    localStorage.setItem(CAT_STORAGE_KEY, JSON.stringify(categories));
}

// Format currency
function formatCurrency(amount) {
    return '₹' + parseFloat(amount).toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
