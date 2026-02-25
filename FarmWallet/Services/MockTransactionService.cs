using System;
using System.Collections.Generic;
using System.Linq;
using FarmWallet.Models;

namespace FarmWallet.Services
{
    public class MockTransactionService
    {
        private List<Transaction> _transactions;

        public MockTransactionService()
        {
            _transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Date = DateTime.Now.AddDays(-10), Type = "Income", Category = "Crop Sales", Amount = 50000, Description = "Wheat sale" },
                new Transaction { Id = 2, Date = DateTime.Now.AddDays(-9), Type = "Expense", Category = "Seeds", Amount = 15000, Description = "Bought wheat seeds" },
                new Transaction { Id = 3, Date = DateTime.Now.AddDays(-8), Type = "Expense", Category = "Fertilizer", Amount = 8000, Description = "Urea and DAP" },
                new Transaction { Id = 4, Date = DateTime.Now.AddDays(-7), Type = "Income", Category = "Dairy Sales", Amount = 12000, Description = "Monthly milk sale" },
                new Transaction { Id = 5, Date = DateTime.Now.AddDays(-6), Type = "Expense", Category = "Labor", Amount = 5000, Description = "Farm hands wages" },
                new Transaction { Id = 6, Date = DateTime.Now.AddDays(-5), Type = "Expense", Category = "Equipment", Amount = 20000, Description = "Tractor repair" },
                new Transaction { Id = 7, Date = DateTime.Now.AddDays(-4), Type = "Income", Category = "Government Subsidy", Amount = 10000, Description = "PM-Kisan subsidy" },
                new Transaction { Id = 8, Date = DateTime.Now.AddDays(-3), Type = "Expense", Category = "Transport", Amount = 3000, Description = "Transporting crops to market" },
                new Transaction { Id = 9, Date = DateTime.Now.AddDays(-2), Type = "Income", Category = "Other Sales", Amount = 4000, Description = "Sold vegetable surplus" },
                new Transaction { Id = 10, Date = DateTime.Now.AddDays(-1), Type = "Expense", Category = "Labor", Amount = 2000, Description = "Harvesting labor" }
            };
        }

        public List<Transaction> GetTransactions()
        {
            return _transactions.OrderByDescending(t => t.Date).ToList();
        }
        
        public void AddTransaction(Transaction transaction)
        {
            transaction.Id = _transactions.Any() ? _transactions.Max(t => t.Id) + 1 : 1;
            _transactions.Add(transaction);
        }
    }
}
