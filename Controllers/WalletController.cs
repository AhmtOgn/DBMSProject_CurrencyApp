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
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            List<Wallet> myWallets = new List<Wallet>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
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
                                    PendingBalance = reader.GetDecimal(reader.GetOrdinal("pendingBalance")), 
                                    CurrencyId = reader.GetInt32(reader.GetOrdinal("currencyId")),
                                    CurrencyCode = reader.GetString(reader.GetOrdinal("currencyCode"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "ERROR:" + ex.Message;
            }

            return View(myWallets); 
        }

        [HttpPost]
        public IActionResult CreateWallet(string currencyCode, string walletName)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
        
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"SELECT ""createWalletF""(@uid, @code, @name)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@code", currencyCode);
                        cmd.Parameters.AddWithValue("@name", (object)walletName ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Wallet created successfully via DB Function!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        public IActionResult DeleteWallet(int walletId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
        
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"SELECT ""deleteWalletF""(@wid, @uid)";
        
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@wid", walletId);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Wallet deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}