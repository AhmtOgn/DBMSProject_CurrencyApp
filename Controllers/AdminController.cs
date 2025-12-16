using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models;
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly DbHelper _dbHelper;

        public AdminController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // 1. Bekleyen İstekleri Listele (Sadece 'Pending' olanlar)
        public IActionResult Index()
        {
            // İstersen buraya admin rolü kontrolü ekleyebilirsin:
            // if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Index", "Home");

            var requests = new List<BankRequest>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    // Kullanıcı adını, cüzdan adını ve banka adını da çekiyoruz (JOIN)
                    string sql = @"
                        SELECT br.*, u.""name"", u.""surname"", b.""bankName"", w.""walletName""
                        FROM ""BankRequest"" br
                        JOIN ""Wallet"" w ON br.""walletId"" = w.""walletId""
                        JOIN ""User"" u ON w.""userId"" = u.""userId""
                        LEFT JOIN ""BankAccount"" b ON br.""bankAccountId"" = b.""bankAccountId""
                        WHERE br.""status"" = 'Pending'
                        ORDER BY br.""date"" ASC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                requests.Add(new BankRequest
                                {
                                    RequestId = reader.GetInt32(reader.GetOrdinal("requestId")),
                                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    // Enum çevrimi
                                    Direction = Enum.Parse<OperationType>(reader.GetString(reader.GetOrdinal("direction"))),
                                    
                                    // Ekranda göstermek için özet bilgi (Modelde WalletName string alanı olmalı)
                                    WalletName = $"{reader.GetString(reader.GetOrdinal("name"))} {reader.GetString(reader.GetOrdinal("surname"))} ({reader.GetString(reader.GetOrdinal("walletName"))})"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading requests: " + ex.Message;
            }

            return View(requests);
        }

        // 2. İsteği Onayla (Approve)
        [HttpPost]
        public IActionResult Approve(int id)
        {
            ProcessRequest(id, "Approved");
            return RedirectToAction("Index");
        }

        // 3. İsteği Reddet (Reject)
        [HttpPost]
        public IActionResult Reject(int id)
        {
            ProcessRequest(id, "Rejected");
            return RedirectToAction("Index");
        }

        // Ortak İşlem Metodu
        private void ProcessRequest(int id, string status)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    // SQL Fonksiyonunu çağırıyoruz
                    using (var cmd = new NpgsqlCommand("SELECT \"processBankRequestF\"(@id, @status)", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = $"Request {status} successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error processing request: " + ex.Message;
            }
        }
    }
}