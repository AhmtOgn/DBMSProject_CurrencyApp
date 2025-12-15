namespace CurrencyApp.Models
{
    public class MarketHistory
    {
        public int HistoryId { get; set; } // PK
        public TimeSpan TimeInterval { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Average { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public int CurrencyPairId { get; set; } // FK
        public DateTime Date { get; set; } = DateTime.Now;
    }
}