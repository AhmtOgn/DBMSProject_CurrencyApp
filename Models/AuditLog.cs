namespace CurrencyApp.Models
{
    public class AuiditLog
    {
        public int AuidtId { get; set; } // PK
        public string Action { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now();
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public TableNames TableName { get; set; }
        public int RecordId { get; set; }
        public int UserId { get; set; } // FK  
    }
}