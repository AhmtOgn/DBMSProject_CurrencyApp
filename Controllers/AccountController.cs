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

                                // Enum verisini string olarak çekelim (role kolonu enum olduğu için GetString çalışmayabilir, object alıp stringe çevirelim)
                                object roleObj = reader["role"];
                                string role = roleObj != null? role = roleObj.ToString()! : "User";

                                object currencyObj = reader["defaultCurrencyCode"];
                                string currency = currencyObj != null ? currencyObj.ToString()! : "USD";

                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserName", $"{name} {surname}");
                                HttpContext.Session.SetString("UserRole", role);
                                HttpContext.Session.SetString("UserCurrency", currency);

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

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User model)
        {
            // Not: ID Number ve Phone unique olmalı, veritabanı hata verirse catch yakalar.
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Please fill the required fiedls!";
                return View(model);
            }

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL INSERT Sorgusu
                    // Not: Role, Status ve DefaultCurrencyCode veritabanında varsayılan (DEFAULT) değere sahip.
                    // Bu yüzden onları göndermiyoruz, veritabanı otomatik atayacak.
                    // Veritabanı otomatik olarak Cüzdan da oluşturacak (Trigger sayesinde).
                    string sql = @"INSERT INTO ""User"" 
                                  (""name"", ""surname"", ""phoneNumber"", ""address"", ""identityNumber"", ""password"", ""email"", ""birthDate"") 
                                  VALUES 
                                  (@name, @surname, @phone, @address, @identity, @pass, @email, @bdate)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@surname", model.Surname);
                        cmd.Parameters.AddWithValue("@phone", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@address", model.Address);
                        cmd.Parameters.AddWithValue("@identity", model.IdentityNumber);
                        cmd.Parameters.AddWithValue("@pass", model.Password); // Without Hashing
                        cmd.Parameters.AddWithValue("@email", model.Email);
                        cmd.Parameters.AddWithValue("@bdate", model.BirthDate);

                        cmd.ExecuteNonQuery(); // Ekleme işlemini çalıştır
                    }
                }

                // Kayıt başarılıysa Login sayfasına yönlendir
                // TempData ile mesaj taşıyabiliriz
                TempData["Success"] = "Registering is succesfull! You can login.";
                return RedirectToAction("Login");
            }
            catch (PostgresException ex)
            {
                // Veritabanı hatalarını yakala (Örn: Aynı email/TC ile kayıt olma)
                if (ex.SqlState == "23505") // Unique violation kodu
                {
                    ViewBag.Error = "Bu E-mail, Telefon veya TC Kimlik zaten kayıtlı!";
                }
                else
                {
                    ViewBag.Error = "Veritabanı hatası: " + ex.Message;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "ERROR: " + ex.Message;
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            // Session'ı temizle (Giriş bilgisini sil)
            HttpContext.Session.Clear();
            
            // Login sayfasına at
            return RedirectToAction("Login");
        }
    }
}