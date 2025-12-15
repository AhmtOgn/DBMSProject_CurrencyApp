using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Models;
using CurrencyApp.Helpers; // DbHelper'ı kullanmak için bunu ekledik
using Npgsql; // PostgreSQL bağlantısı için

namespace CurrencyApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DbHelper _dbHelper;
    public HomeController(ILogger<HomeController> logger, DbHelper dbHelper)
    {
        _logger = logger;
        _dbHelper = dbHelper;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}