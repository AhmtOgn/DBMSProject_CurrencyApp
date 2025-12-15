namespace CurrencyApp.Models
{
    public class BankAccount
    {
        public int BankAccountId { get; set; } // PK // U
        public string BankName { get; set; } = "";
        public string IBAN { get; set; } = "";
        public string BaseCountry { get; set; } = "";
        public int UserId { get; set; } // FK
    }
}