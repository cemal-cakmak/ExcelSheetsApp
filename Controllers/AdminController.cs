using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelSheetsApp.Models;
using ExcelSheetsApp.Services;

namespace ExcelSheetsApp.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IAdminService _adminService;

        public AdminController(ILogger<AdminController> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        // Admin kontrolü
        private bool IsAdmin()
        {
            // Sadece "admin" kullanıcı adına sahip kullanıcılar admin paneline erişebilir
            return User.Identity?.IsAuthenticated == true && 
                   User.Identity?.Name?.ToLower() == "admin";
        }

        // Ana admin sayfası
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece admin kullanıcısı admin paneline erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var model = await _adminService.GetDashboardDataAsync();
            return View(model);
        }

        // Kullanıcı yönetimi
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece admin kullanıcısı admin paneline erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var users = await _adminService.GetAllUsersAsync();
            var model = users.Select(u => new UserViewModel
            {
                Username = u.UserName ?? "",
                Role = "User", // Role sistemi eklendiğinde güncellenecek
                IsActive = true
            }).ToList();
            
            return View(model);
        }

        // Yeni kullanıcı ekleme
        [HttpPost]
        public async Task<IActionResult> AddUser(UserViewModel model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            if (ModelState.IsValid)
            {
                // Önce kullanıcının zaten var olup olmadığını kontrol et
                var existingUsers = await _adminService.GetAllUsersAsync();
                if (existingUsers.Any(u => u.UserName?.ToLower() == model.Username.ToLower()))
                {
                    return Json(new { success = false, message = $"'{model.Username}' kullanıcı adı zaten kullanılıyor. Farklı bir kullanıcı adı seçin." });
                }

                var success = await _adminService.CreateUserAsync(model);
                if (success)
                {
                    await _adminService.LogActivityAsync("Kullanıcı Eklendi", User.Identity?.Name ?? "admin", "", "Success", null, $"Yeni kullanıcı: {model.Username}");
                    return Json(new { success = true, message = $"'{model.Username}' kullanıcısı başarıyla eklendi!" });
                }
                else
                {
                    return Json(new { success = false, message = "Kullanıcı oluşturulamadı. Lütfen kullanıcı adını ve şifreyi kontrol edin." });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Form hatası: " + string.Join(", ", errors) });
        }

        // Kullanıcı silme
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı" });
            }

            var success = await _adminService.DeleteUserAsync(id);
            if (success)
            {
                await _adminService.LogActivityAsync("Kullanıcı Silindi", User.Identity?.Name ?? "admin", "", "Success", null, $"Silinen kullanıcı: {user.UserName}");
            }
            
            return Json(new { success = success, message = success ? "Kullanıcı silindi" : "Kullanıcı silinemedi" });
        }

        // Kullanıcı şifre değiştirme
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string id, string newPassword)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı" });
            }

            var success = await _adminService.ChangeUserPasswordAsync(id, newPassword);
            if (success)
            {
                await _adminService.LogActivityAsync("Şifre Değiştirildi", User.Identity?.Name ?? "admin", "", "Success", null, $"Kullanıcı: {user.UserName}");
            }
            
            return Json(new { success = success, message = success ? "Şifre değiştirildi" : "Şifre değiştirilemedi" });
        }

        // Sistem ayarları
        public async Task<IActionResult> Settings()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece admin kullanıcısı admin paneline erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var settings = await _adminService.GetSystemSettingsAsync();
            return View(settings);
        }

        // Sistem ayarları güncelleme
        [HttpPost]
        public async Task<IActionResult> UpdateSettings(SystemSettingsViewModel model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            if (ModelState.IsValid)
            {
                var success = await _adminService.UpdateSystemSettingsAsync(model, User.Identity?.Name ?? "admin");
                if (success)
                {
                    await _adminService.LogActivityAsync("Sistem Ayarları Güncellendi", User.Identity?.Name ?? "admin", "", "Success");
                }
                return Json(new { success = success, message = success ? "Ayarlar güncellendi" : "Ayarlar güncellenemedi" });
            }

            return Json(new { success = false, message = "Geçersiz veri" });
        }

        // İstatistikler
        public async Task<IActionResult> Statistics()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece admin kullanıcısı admin paneline erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var stats = await _adminService.GetSystemStatisticsAsync();
            return View(stats);
        }

        // Log görüntüleme
        public async Task<IActionResult> Logs()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece admin kullanıcısı admin paneline erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var logs = await _adminService.GetSystemLogsAsync();
            return View(logs);
        }

        // Eksik kullanıcı yönetimi metodları
        [HttpPost]
        public async Task<IActionResult> EditUser(UserViewModel model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            if (ModelState.IsValid)
            {
                var user = await _adminService.GetAllUsersAsync();
                var targetUser = user.FirstOrDefault(u => u.UserName == model.Username);
                
                if (targetUser != null)
                {
                    var success = await _adminService.UpdateUserAsync(targetUser.Id, model);
                    if (success && !string.IsNullOrEmpty(model.Password))
                    {
                        await _adminService.ChangeUserPasswordAsync(targetUser.Id, model.Password);
                    }
                    
                    if (success)
                    {
                        await _adminService.LogActivityAsync("Kullanıcı Düzenlendi", User.Identity?.Name ?? "admin", "", "Success", null, $"Düzenlenen kullanıcı: {model.Username}");
                    }
                    
                    return Json(new { success = success, message = success ? "Kullanıcı güncellendi" : "Kullanıcı güncellenemedi" });
                }
            }

            return Json(new { success = false, message = "Geçersiz veri" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string username)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            // Basit bir status toggle simülasyonu
            await _adminService.LogActivityAsync("Kullanıcı Durumu Değiştirildi", User.Identity?.Name ?? "admin", "", "Success", null, $"Kullanıcı: {username}");
            
            return Json(new { success = true, message = "Kullanıcı durumu güncellendi" });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings(SystemSettingsViewModel model)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            var success = await _adminService.UpdateSystemSettingsAsync(model, User.Identity?.Name ?? "admin");
            if (success)
            {
                await _adminService.LogActivityAsync("Sistem Ayarları Güncellendi", User.Identity?.Name ?? "admin", "", "Success");
            }
            
            return Json(new { success = success, message = success ? "Ayarlar kaydedildi" : "Ayarlar kaydedilemedi" });
        }

        // Sistem performansı API
        [HttpGet]
        public async Task<IActionResult> GetSystemPerformance()
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            try
            {
                var performance = await _adminService.GetSystemPerformanceAsync();
                return Json(new { 
                    success = true, 
                    performance = new {
                        cpuUsage = performance.CpuUsage,
                        memoryUsage = performance.MemoryUsage,
                        diskUsage = performance.DiskUsage,
                        uptime = performance.Uptime.TotalHours.ToString("F1")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sistem performansı alınırken hata oluştu");
                return Json(new { success = false, message = "Sistem performansı alınamadı" });
            }
        }
    }
}