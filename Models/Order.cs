namespace CurrencyApp.Models
{
    public class Order
    {
        public int OrderId { get; set; } // PK // U
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderType OrdType { get; set; } // (Market, Limit)
        public OperationType OpeType { get; set; } // (Sell, Buy, ////Deposit, Withdrawal)
        public ProcessStatus OrderStatus { get; set; } // (Pending, Approved, Rejected, Completed, Cancelled, Expired)
        public decimal OrderFee { get; set; }
        public decimal TargetAmount { get; set; } // Alınacak miktar
        public decimal SourceAmount { get; set; } // Harcanan miktar
        public int UserId { get; set; } // FK
        public int CurrencyPairId { get; set; } // FK

        //Extra
        public string PairName { get; set; } = ""; // Örn: BTC/USD
    }
}