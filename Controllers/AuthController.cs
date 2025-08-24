using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ExcelSheetsApp.Models;

namespace ExcelSheetsApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Eğer zaten giriş yapmışsa ana sayfaya yönlendir
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Basit kullanıcı doğrulama (ileride veritabanından kontrol edilecek)
            if (IsValidUser(model.Username, model.Password))
            {
                // Session'a kullanıcı bilgilerini kaydet
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("Username", model.Username);
                HttpContext.Session.SetString("UserRole", GetUserRole(model.Username));

                _logger.LogInformation($"Kullanıcı giriş yaptı: {model.Username}");
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı!");
            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Session'ı temizle
            HttpContext.Session.Clear();
            _logger.LogInformation("Kullanıcı çıkış yaptı");
            return RedirectToAction("Login");
        }

        private bool IsValidUser(string username, string password)
        {
            // Basit kullanıcı listesi (ileride veritabanından gelecek)
            var users = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "user1", "user123" },
                { "user2", "user456" }
            };

            return users.ContainsKey(username) && users[username] == password;
        }

        private string GetUserRole(string username)
        {
            // Basit rol kontrolü
            return username == "admin" ? "Admin" : "User";
        }
    }
}
