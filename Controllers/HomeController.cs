using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Models;
using CurrencyApp.Helpers;
using Npgsql;

namespace CurrencyApp.Controllers;

public class HomeController : Controller
{
    private readonly DbHelper _dbHelper;

    public HomeController(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public IActionResult Index()
    {
        // Oturum kontrolü (Giriş yapmamışsa Login'e at)
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        decimal totalBalance = 0;
        List<Wallet> myWallets = new List<Wallet>();

        try
        {
            using (var connection = _dbHelper.GetConnection())
            {
                connection.Open();

                // 1. TOPLAM VARLIĞI HESAPLA (SQL Fonksiyonunu Çağır)
                using (var cmd = new NpgsqlCommand("SELECT getTotalBalance(@uid)", connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    var result = cmd.ExecuteScalar(); // Tek bir değer dönecek
                    if (result != DBNull.Value)
                    {
                        totalBalance = Convert.ToDecimal(result);
                    }
                }

                // 2. CÜZDANLARI LİSTELE (Bakiyesi 0'dan büyük olanları getir)
                // Currency tablosuyla JOIN yaparak para birimi kodunu (USD, TRY) alıyoruz.
                string walletSql = @"
                    SELECT w.*, c.""currencyCode""
                    FROM ""Wallet"" w
                    JOIN ""Currency"" c ON w.""currencyId"" = c.""currencyId""
                    WHERE w.""userId"" = @uid AND (w.""balance"" > 0 OR w.""pendingBalance"" > 0)
                    LIMIT 3"; // Sadece ilk 3 tanesini gösterelim (Özet)

                using (var cmd = new NpgsqlCommand(walletSql, connection))
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
                                CurrencyCode = reader.GetString(reader.GetOrdinal("currencyCode"))
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Veri çekilirken hata oluştu: " + ex.Message;
        }

        // Verileri View'a taşıyoruz
        ViewBag.TotalBalance = totalBalance;
        ViewBag.Currency = HttpContext.Session.GetString("UserCurrency") ?? "USD";
        
        return View(myWallets);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}