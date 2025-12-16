using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models;
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class WalletController : Controller
    {
        private readonly DbHelper _dbHelper;

        public WalletController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IActionResult Index()
        {
            // Oturum Kontrolü
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            List<Wallet> myWallets = new List<Wallet>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL JOIN SORGUSU
                    // Wallet tablosundaki currencyId ile Currency tablosunu birleştiriyoruz.
                    // Böylece "USD", "TRY" gibi kodları alabiliyoruz.
                    string sql = @"
                        SELECT w.*, c.""currencyCode"", c.""currencyName""
                        FROM ""Wallet"" w
                        JOIN ""Currency"" c ON w.""currencyId"" = c.""currencyId""
                        WHERE w.""userId"" = @uid
                        ORDER BY w.""walletId"" ASC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                myWallets.Add(new Wallet
                                {
                                    WalletId = reader.GetInt32(reader.GetOrdinal("walletId")),
                                    WalletName = reader.GetString(reader.GetOrdinal("walletName")),
                                    Balance = reader.GetDecimal(reader.GetOrdinal("balance")),
                                    PendingBalance = reader.GetDecimal(reader.GetOrdinal("pendingBalance")), // Dondurulmuş Bakiye
                                    CurrencyId = reader.GetInt32(reader.GetOrdinal("currencyId")),
                                    CurrencyCode = reader.GetString(reader.GetOrdinal("currencyCode")) // Modelde bu alan olmalı
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading wallets: " + ex.Message;
            }

            return View(myWallets);
        }
    }
}