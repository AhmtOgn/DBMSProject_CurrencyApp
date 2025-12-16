using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models; // Transaction modelinin burada olduğundan emin ol
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class TransactionController : Controller
    {
        private readonly DbHelper _dbHelper;

        public TransactionController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            List<Transaction> transactions = new List<Transaction>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL JOIN: 
                    // Transaction -> Wallet (Kime ait?) -> Currency (Hangi para?)
                    // Sadece giriş yapan kullanıcıya ait (w."userId" = @uid) kayıtları getir.
                    string sql = @"
                        SELECT t.*, c.""currencyCode""
                        FROM ""Transaction"" t
                        JOIN ""Wallet"" w ON t.""walletId"" = w.""walletId""
                        JOIN ""Currency"" c ON w.""currencyId"" = c.""currencyId""
                        WHERE w.""userId"" = @uid
                        ORDER BY t.""date"" DESC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Veritabanından string olarak okuyoruz
                                string opString = reader.GetString(reader.GetOrdinal("operationType"));
                                string statusString = reader.GetString(reader.GetOrdinal("transactionStatus"));
                            
                                transactions.Add(new Transaction
                                {
                                    TransactionId = reader.GetInt32(reader.GetOrdinal("transactionId")),
                                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    
                                    // HATA DÜZELTME 1: Enum.Parse Kullanımı
                                    // Gelen stringi (Örn: "Buy") Enum tipine (OperationType.Buy) çevirir.
                                    OperationType = Enum.Parse<OperationType>(opString),
                                    TransactionStatus = Enum.Parse<ProcessStatus>(statusString),
                            
                                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                    OperationFee = reader.IsDBNull(reader.GetOrdinal("operationFee")) ? 0 : reader.GetDecimal(reader.GetOrdinal("operationFee"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading history: " + ex.Message;
            }

            return View(transactions);
        }
    }
}