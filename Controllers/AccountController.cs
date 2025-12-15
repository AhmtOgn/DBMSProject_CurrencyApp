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

        // --- PROFİL DÜZENLEME (EDIT PROFILE) ---

        // GET: Bilgileri Getir
        // GET: Bilgileri Getir
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            User currentUser = new User();
            List<Currency> currencyList = new List<Currency>(); // Para birimlerini tutacak liste

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // 1. KULLANICI BİLGİLERİNİ ÇEK
                    string userSql = @"SELECT * FROM ""User"" WHERE ""userId"" = @uid";
                    using (var cmd = new NpgsqlCommand(userSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUser.UserId = reader.GetInt32(reader.GetOrdinal("userId"));
                                currentUser.Name = reader.GetString(reader.GetOrdinal("name"));
                                currentUser.Surname = reader.GetString(reader.GetOrdinal("surname"));
                                currentUser.Email = reader.GetString(reader.GetOrdinal("email"));
                                currentUser.PhoneNumber = reader.GetString(reader.GetOrdinal("phoneNumber"));
                                currentUser.Address = reader.GetString(reader.GetOrdinal("address"));
                                currentUser.IdentityNumber = reader.GetString(reader.GetOrdinal("identityNumber"));
                                currentUser.Password = reader.GetString(reader.GetOrdinal("password"));
                                currentUser.BirthDate = reader.GetDateTime(reader.GetOrdinal("birthDate"));
                                
                                // Default Currency'yi çekiyoruz
                                currentUser.DefaultCurrencyCode = reader.GetString(reader.GetOrdinal("defaultCurrencyCode"));
                            }
                        }
                    }

                    // 2. PARA BİRİMLERİNİ ÇEK (Aynı bağlantıyı kullanıyoruz)
                    // Önceki Reader kapandığı için yeni komut çalıştırabiliriz.
                    string currencySql = @"SELECT ""currencyCode"", ""currencyName"" FROM ""Currency"" ORDER BY ""currencyCode"" ASC";
                    using (var cmd = new NpgsqlCommand(currencySql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currencyList.Add(new Currency
                                {
                                    CurrencyCode = reader.GetString(reader.GetOrdinal("currencyCode")),
                                    CurrencyName = reader.GetString(reader.GetOrdinal("currencyName"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error fetching data: " + ex.Message;
            }

            // Listeyi View'a taşıyoruz
            ViewBag.Currencies = currencyList;

            return View(currentUser);
        }

        // POST: Bilgileri Güncelle
        [HttpPost]
        public IActionResult Profile(User model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();

                    // SQL UPDATE Sorgusu (defaultCurrencyCode eklendi)
                    string sql = @"UPDATE ""User"" 
                                   SET ""name"" = @name, 
                                       ""surname"" = @surname, 
                                       ""phoneNumber"" = @phone, 
                                       ""address"" = @addr, 
                                       ""password"" = @pass,
                                       ""defaultCurrencyCode"" = @currency
                                   WHERE ""userId"" = @uid";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@surname", model.Surname);
                        cmd.Parameters.AddWithValue("@phone", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@addr", model.Address ?? "");
                        cmd.Parameters.AddWithValue("@pass", model.Password);
                        
                        // YENİ: Para birimini ekle
                        cmd.Parameters.AddWithValue("@currency", model.DefaultCurrencyCode);
                        
                        cmd.Parameters.AddWithValue("@uid", userId);

                        cmd.ExecuteNonQuery();
                    }
                }

                // 1. Session'daki İsim bilgisini güncelle
                HttpContext.Session.SetString("UserName", $"{model.Name} {model.Surname}");
                
                // 2. YENİ: Session'daki Para Birimini Güncelle (Böylece Dashboard anında değişir)
                HttpContext.Session.SetString("UserCurrency", model.DefaultCurrencyCode);

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Index" , "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Update failed: " + ex.Message;
                return View(model);
            }
        }
    }
}