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

                //Calculate total balance
                using (var cmd = new NpgsqlCommand("SELECT getTotalBalance(@uid)", connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        totalBalance = Convert.ToDecimal(result);
                    }
                }

                //List wallets who have money
                string walletSql = @"
                    SELECT w.*, c.""currencyCode""
                    FROM ""Wallet"" w
                    JOIN ""Currency"" c ON w.""currencyId"" = c.""currencyId""
                    WHERE w.""userId"" = @uid AND (w.""balance"" > 0 OR w.""pendingBalance"" > 0)
                    LIMIT 3"; //Display just top three

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

        ViewBag.TotalBalance = totalBalance;
        ViewBag.Currency = HttpContext.Session.GetString("UserCurrency") ?? "USD";
        
        return View(myWallets);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}