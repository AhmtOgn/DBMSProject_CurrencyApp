namespace CurrencyApp.Models
{
    public class BankRequest
    {
        public int RequestId { get; set; } // PK // U
        public OperationType Direction { get; set; } // (Deposit, Withdrawal ////SELL, BUY)
        public decimal Amount { get; set; }
        public ProcessStatus Status { get; set; } = ProcessStatus.Pending; // (Pending, Approved, Rejected, Completed, Cancelled, Expired)
        public DateTime Date { get; set; } = DateTime.Now;
        public int BankAccountId { get; set; } // FK
        public int WalletId { get; set; } // FK
        
        //Extra
        public string? BankName { get; set; } // Hangi bankadan?
        public string? WalletName { get; set; }
        public string? UserName { get; set; } // Kim istemiş? (Admin paneli için)
    }
}