namespace CurrencyApp.Models
{
    public class Wallet
    {
        public int WalletId { get; set; } // PK // U
        public string WalletName { get; set; } = "";
        public decimal Balance { get; set; }
        public decimal PendingBalance { get; set; }
        public int UserId { get; set; } // FK
        public int CurrencyId { get; set; } // FK

        //Extra
        public string CurrencyCode { get; set; } = "";
    }
}