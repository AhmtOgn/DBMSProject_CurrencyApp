namespace CurrencyApp.Models
{
    public enum UserRole
    {
        User,
        Admin
    }
    
    public enum UserStatus
    {
        NonValid,
        ValidPhone,
        ValidID
    }

    public enum OperationType
    {
        Sell,
        Buy,
        Deposit,
        Withdrawal
    }

    public enum OrderType
    {
        Market,
        Limit
    }

    public enum ProcessStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed,
        Cancelled,
        Expired
    }

    public enum TableNames
    {
        User,
        AuiditLog,
        Currency,
        CurrencyPair,
        MarketHistory,
        Wallet,
        BankAccount,
        BankRequest,
        Order,
        MarketOrder,
        LimitOrder,
        Transaction
    }
}