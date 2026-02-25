// js/data.js
// Handles Data Initialization and LocalStorage Operations

const STORAGE_KEY = 'farmWalletTransactions';
const CAT_STORAGE_KEY = 'farmWalletCategories';

// Default mock data if empty
const defaultTransactions = [];

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
