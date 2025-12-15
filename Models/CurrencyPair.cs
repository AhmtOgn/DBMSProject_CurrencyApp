namespace CurrencyApp.Models
{
    public class CurrencyPair
    {
        public int CurrencyPairId { get; set; } // PK
        public decimal Rate { get; set; }
        public decimal PreviousClose { get; set; }
        public int BaseCurrencyId { get; set; } // FK
        public int TargetCurrencyId { get; set; } // FK

        // Extra
        public string BaseCurrencyCode { get; set; } = "";
        public string TargetCurrencyCode { get; set; } = "";
        public string PairName => $"{BaseCurrencyCode}/{TargetCurrencyCode}"; // U
    }
}