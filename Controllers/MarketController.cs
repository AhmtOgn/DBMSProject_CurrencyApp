using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models;
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class MarketController : Controller
    {
        private readonly DbHelper _dbHelper;

        public MarketController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // 1. PİYASALARI LİSTELE
        public IActionResult Index()
        {
            // Giriş kontrolü
            if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("Login", "Account");

            List<CurrencyPair> pairs = new List<CurrencyPair>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL JOIN: Currency tablosuna iki kere bağlanıyoruz (Biri Base, Biri Target için)
                    // Böylece ID'ler yerine 'USD', 'TRY' gibi kodları alabiliyoruz.
                    string sql = @"
                        SELECT cp.*, 
                               c1.""currencyCode"" as BaseCode, 
                               c2.""currencyCode"" as TargetCode
                        FROM ""CurrencyPair"" cp
                        JOIN ""Currency"" c1 ON cp.""baseCurrencyId"" = c1.""currencyId""
                        JOIN ""Currency"" c2 ON cp.""targetCurrencyId"" = c2.""currencyId""
                        ORDER BY cp.""currencyPairId"" ASC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var pair = new CurrencyPair
                                {
                                    CurrencyPairId = reader.GetInt32(reader.GetOrdinal("currencyPairId")),
                                    Rate = reader.GetDecimal(reader.GetOrdinal("rate")),
                                    BaseCurrencyId = reader.GetInt32(reader.GetOrdinal("baseCurrencyId")),
                                    TargetCurrencyId = reader.GetInt32(reader.GetOrdinal("targetCurrencyId")),
                                    
                                    // Join ile gelen verileri elle eşleştiriyoruz
                                    BaseCurrencyCode = reader.GetString(reader.GetOrdinal("BaseCode")),
                                    TargetCurrencyCode = reader.GetString(reader.GetOrdinal("TargetCode"))
                                };
                                pairs.Add(pair);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading markets: " + ex.Message;
            }

            return View(pairs);
        }

        // 2. AL/SAT EKRANI (TRADE SCREEN)
        [HttpGet]
        public IActionResult Trade(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("Login", "Account");

            CurrencyPair? selectedPair = null;

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    // Seçilen paritenin detaylarını çek
                    string sql = @"
                        SELECT cp.*, c1.""currencyCode"" as BaseCode, c2.""currencyCode"" as TargetCode
                        FROM ""CurrencyPair"" cp
                        JOIN ""Currency"" c1 ON cp.""baseCurrencyId"" = c1.""currencyId""
                        JOIN ""Currency"" c2 ON cp.""targetCurrencyId"" = c2.""currencyId""
                        WHERE cp.""currencyPairId"" = @id";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                selectedPair = new CurrencyPair
                                {
                                    CurrencyPairId = reader.GetInt32(reader.GetOrdinal("currencyPairId")),
                                    Rate = reader.GetDecimal(reader.GetOrdinal("rate")),
                                    BaseCurrencyId = reader.GetInt32(reader.GetOrdinal("baseCurrencyId")),
                                    TargetCurrencyId = reader.GetInt32(reader.GetOrdinal("targetCurrencyId")),
                                    BaseCurrencyCode = reader.GetString(reader.GetOrdinal("BaseCode")),
                                    TargetCurrencyCode = reader.GetString(reader.GetOrdinal("TargetCode"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
            }

            if (selectedPair == null) return RedirectToAction("Index");

            return View(selectedPair);
        }

        [HttpPost]
        public IActionResult ExecuteTrade(int currencyPairId, string operationType, decimal amount)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (amount <= 0)
            {
                TempData["Error"] = "Amount must be greater than zero.";
                return RedirectToAction("Trade", new { id = currencyPairId });
            }

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL Fonksiyonunu Çağırıyoruz
                    string sql = "SELECT \"executeTradeF\"(@uid, @pid, @op::operation_type, @amt)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pid", currencyPairId);
                        cmd.Parameters.AddWithValue("@op", operationType);
                        cmd.Parameters.AddWithValue("@amt", amount);

                        cmd.ExecuteNonQuery();
                    }
                }
                
                TempData["Success"] = "Transaction completed successfully!";
                
                // DEĞİŞİKLİK BURADA: Başarılıysa Piyasalar Listesine (Index) dön
                return RedirectToAction("Index");
            }
            catch (PostgresException ex)
            {
                TempData["Error"] = "Transaction failed: " + ex.MessageText;
                // Hata varsa işlem ekranında kal
                return RedirectToAction("Trade", new { id = currencyPairId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "System error: " + ex.Message;
                // Hata varsa işlem ekranında kal
                return RedirectToAction("Trade", new { id = currencyPairId });
            }
        }
    }
}