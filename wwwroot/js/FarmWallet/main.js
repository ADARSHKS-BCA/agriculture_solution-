// js/main.js
// Application Entry Point

document.addEventListener('DOMContentLoaded', () => {
    // 1. Initialize Default Data (if empty)
    initData();

    // 2. Populate Dropdowns (Years, Categories)
    populateYearDropdown();
    updateCategoryDropdown();
    renderCategoryLists();

    // 3. Render Dashboard
    updateDashboard();

    console.log("Farm Wallet Dashboard enhanced with JS features initialized successfully.");
});
