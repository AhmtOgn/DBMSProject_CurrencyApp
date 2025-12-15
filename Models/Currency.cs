namespace CurrencyApp.Models
{
    public class Currency
    {
        public int CurrencyId { get; set; } //PK // U
        public string CurrencyName { get; set; } = "";
        public string CurrencyCode { get; set; } = ""; // U
        public string BaseCountry { get; set; } = "";
    }
}