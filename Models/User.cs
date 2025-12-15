namespace CurrencyApp.Models
{
    public class User
    {
        public int UserId { get; set; } // PK // U
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string PhoneNumber { get; set; } = ""; // U
        public string Address { get; set; } = "";
        public string IdentityNumber { get; set; } = ""; // U
        public string Password { get; set; } = "";
        public string Email { get; set; } = ""; // Email must be in mail format // U
        public DateTime BirthDate { get; set; } // User must be older than 18 years
        public UserStatus MembershipStatus { get; set; } = UserStatus.NonValid; // (NonValid, ValidPhone, ValidId)
        public UserRole Role { get; set; } = UserRole.User; // (User, Admin)
        public string DefaultCurrencyCode { get; set; } = "USD";
    }
}