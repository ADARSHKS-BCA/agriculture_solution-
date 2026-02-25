using System;

namespace FarmWallet.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } // Income, Expense
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
