namespace CurrencyApp.Models
{
   public class LimitOrder : Order
    {
        public DateTime EndTime { get; set; }
        public decimal LimitPrice { get; set; }
    }
}