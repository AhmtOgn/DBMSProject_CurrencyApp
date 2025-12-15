using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models;
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbHelper _dbHelper;

        public AccountController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // GET: /Account/Login (Sayfayı Gösterir)
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login (Form Gönderilince Çalışır)
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Basit validasyon
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please Fill all the fileds!";
                return View();
            }

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    
                    string sql = @"SELECT * FROM ""User"" 
                                   WHERE ""email"" = @pEmail AND ""password"" = @pPass";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        // SQL Injection önlemek için parametre kullanıyoruz
                        cmd.Parameters.AddWithValue("@pEmail", email);
                        cmd.Parameters.AddWithValue("@pPass", password); // Without Hashing

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {   
                                // User was found
                                // 1. Bilgileri Session'a Kaydet
                                // Veritabanından gelen userId ve diğer bilgileri alıyoruz
                                int userId = reader.GetInt32(reader.GetOrdinal("userId"));
                                string name = reader.GetString(reader.GetOrdinal("name"));
                                string surname = reader.GetString(reader.GetOrdinal("surname"));
                                string role = "User"; // Varsayılan

                                // Enum verisini string olarak çekelim (role kolonu enum olduğu için GetString çalışmayabilir, object alıp stringe çevirelim)
                                object roleObj = reader["role"];
                                if(roleObj != null) role = roleObj.ToString();

                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserName", $"{name} {surname}");
                                HttpContext.Session.SetString("UserRole", role);

                                // 2. Ana Sayfaya Yönlendir
                                return RedirectToAction("Index", "Home");
                            }
                            else
                            {
                                // User wasn't found
                                ViewBag.Error = "Incorrect Email or Password, please try again!";
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "ERROR: " + ex.Message;
                return View();
            }
        }

        // Çıkış Yapma (Logout)
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Tüm oturum bilgilerini sil
            return RedirectToAction("Login");
        }
    }
}