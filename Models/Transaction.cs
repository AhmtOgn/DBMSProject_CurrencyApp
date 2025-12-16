namespace CurrencyApp.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; } // PK // U
        public DateTime Date { get; set; } = DateTime.Now;
        public OperationType OperationType { get; set; } // (Sell, Buy, Deposit, Withdrawal
        public ProcessStatus TransactionStatus { get; set; }  // (Pending, Approved, Rejected, Completed, Cancelled, Expired)
        public decimal OperationFee { get; set; }
        public decimal Amount { get; set; }
        public int WalletId { get; set; } // FK
        public int? OrderId { get; set; } // FK
        
        //Extra
        public string? CurrencyCode { get; set; }
    }
}