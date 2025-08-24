using System.Diagnostics;
using System.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using ExcelSheetsApp.Models;
using ExcelSheetsApp.Services;
using ExcelSheetsApp.Hubs;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ExcelSheetsApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SeleniumService? _seleniumService;
    private readonly IAdminService _adminService;

    public HomeController(ILogger<HomeController> logger, IAdminService adminService, SeleniumService? seleniumService = null)
    {
        _logger = logger;
        _seleniumService = seleniumService;
        _adminService = adminService;
    }

    public IActionResult Index()
    {
        var model = new HomeViewModel();
        var sessionFilePath = HttpContext.Session.GetString("UploadedExcelPath");
        if (!string.IsNullOrWhiteSpace(sessionFilePath) && System.IO.File.Exists(sessionFilePath))
        {
            try
            {
                using var workbook = new XLWorkbook(sessionFilePath);
                model.UploadedFilePath = sessionFilePath;
                model.Sheets = workbook.Worksheets.Select(ws => ws.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel dosyası okunurken hata oluştu.");
                model.Message = "Excel dosyası okunamadı.";
            }
        }
        return View(model);
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

    [HttpPost]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        var startTime = DateTime.Now;
        
        if (file == null || file.Length == 0)
        {
            TempData["Message"] = "Lütfen bir Excel dosyası seçin.";
            // Log hatalı upload denemesi
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Excel Upload", User.Identity?.Name ?? "Unknown", "", "Failed", 0, "Dosya seçilmedi"));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var uploadsRoot = Path.Combine(Path.GetTempPath(), "ExcelSheetsAppUploads");
            Directory.CreateDirectory(uploadsRoot);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var savedPath = Path.Combine(uploadsRoot, fileName);
            using (var stream = System.IO.File.Create(savedPath))
            {
                file.CopyTo(stream);
            }

            HttpContext.Session.SetString("UploadedExcelPath", savedPath);
            TempData["Message"] = "Excel dosyası başarıyla yüklendi.";
            
            // Log başarılı upload
            var processingTime = (DateTime.Now - startTime).TotalSeconds;
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Excel Upload", User.Identity?.Name ?? "Unknown", Path.GetFileName(file.FileName), "Success", processingTime, $"Dosya boyutu: {file.Length} bytes"));
            _ = Task.Run(async () => await _adminService.IncrementFileTypeStatisticAsync(Path.GetExtension(file.FileName), file.Length));
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel dosyası yüklenirken hata oluştu.");
            TempData["Message"] = "Excel dosyası yüklenirken hata oluştu.";
            
            // Log hatalı upload
            var processingTime = (DateTime.Now - startTime).TotalSeconds;
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Excel Upload", User.Identity?.Name ?? "Unknown", Path.GetFileName(file.FileName), "Failed", processingTime, $"Hata: {ex.Message}"));
            
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult UploadExcelAjax(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Lütfen bir Excel dosyası seçin." });
        }

        try
        {
            var uploadsRoot = Path.Combine(Path.GetTempPath(), "ExcelSheetsAppUploads");
            Directory.CreateDirectory(uploadsRoot);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var savedPath = Path.Combine(uploadsRoot, fileName);
            using (var stream = System.IO.File.Create(savedPath))
            {
                file.CopyTo(stream);
            }

            HttpContext.Session.SetString("UploadedExcelPath", savedPath);

            // Excel sayfalarını oku
            var sheetNames = new List<string>();
            using var workbook = new XLWorkbook(savedPath);
            sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();

            return Json(new { 
                success = true, 
                message = "Excel dosyası başarıyla yüklendi.",
                fileName = Path.GetFileName(file.FileName),
                sheetNames = sheetNames,
                totalSheets = sheetNames.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel dosyası yüklenirken hata oluştu.");
            return Json(new { success = false, message = "Excel dosyası yüklenirken hata oluştu: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult TestData()
    {
        var testData = new List<object>
        {
            new { siraNo = "1", soru = "Test Soru 1", cevap = "Test Cevap 1", aciklama = "Test Açıklama 1" },
            new { siraNo = "2", soru = "Test Soru 2", cevap = "Test Cevap 2", aciklama = "Test Açıklama 2" },
            new { siraNo = "3", soru = "Test Soru 3", cevap = "Test Cevap 3", aciklama = "Test Açıklama 3" }
        };

        return Json(new { 
            success = true, 
            excelData = testData,
            totalSheets = 1,
            completedSheets = new List<string>(),
            selectedSheet = "TestSheet",
            message = "Test verisi başarıyla yüklendi."
        });
    }

    [HttpPost]
    public async Task<IActionResult> LoadSheet(string selectedSheet)
    {
        var startTime = DateTime.Now;
        var model = new HomeViewModel();
        var sessionFilePath = HttpContext.Session.GetString("UploadedExcelPath");
        
        if (string.IsNullOrWhiteSpace(sessionFilePath) || !System.IO.File.Exists(sessionFilePath))
        {
            model.Message = "Önce bir Excel dosyası yükleyin.";
            // Log hatalı sheet load denemesi
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Sheet Load", User.Identity?.Name ?? "Unknown", "", "Failed", 0, "Excel dosyası bulunamadı"));
            return View("Index", model);
        }

        try
        {
            using var workbook = new XLWorkbook(sessionFilePath);
            model.UploadedFilePath = sessionFilePath;
            model.Sheets = workbook.Worksheets.Select(ws => ws.Name).ToList();
            model.SelectedSheet = selectedSheet;
            model.TotalSheets = model.Sheets.Count;
            
            // Seçilen sheet'i session'a kaydet
            HttpContext.Session.SetString("SelectedSheet", selectedSheet);
            
            // Çoklu sayfa modu için session'ları güncelle
            var currentIndex = model.Sheets.IndexOf(selectedSheet);
            HttpContext.Session.SetString("CurrentSheetIndex", currentIndex.ToString());
            HttpContext.Session.SetString("TotalSheets", model.TotalSheets.ToString());
            
            // Tamamlanan sayfaları session'dan al
            var completedSheetsJson = HttpContext.Session.GetString("CompletedSheets");
            if (!string.IsNullOrEmpty(completedSheetsJson))
            {
                model.CompletedSheets = System.Text.Json.JsonSerializer.Deserialize<List<string>>(completedSheetsJson) ?? new();
            }
            
            model.CurrentSheetIndex = currentIndex;

            var ws = workbook.Worksheet(selectedSheet);
            if (ws == null)
            {
                model.Message = "Sayfa bulunamadı.";
                return View("Index", model);
            }

            var range = ws.RangeUsed();
            if (range == null)
            {
                model.Message = "Sayfa boş.";
                return View("Index", model);
            }

            // Excel verilerini oku
            var excelData = new List<ExcelDataRow>();
            var firstRow = range.FirstRowUsed();
            var firstColumn = range.FirstColumnUsed().ColumnNumber();
            var lastColumn = range.LastColumnUsed().ColumnNumber();
            var headerRow = firstRow.RowNumber();
            var dataStartRow = headerRow + 1;
            var lastRow = range.LastRowUsed().RowNumber();

            // Header bilgilerini oku
            var headers = new List<string>();
            for (int col = firstColumn; col <= lastColumn && col <= firstColumn + 5; col++)
            {
                var headerText = ws.Cell(headerRow, col).GetValue<string>() ?? $"Sütun {col - firstColumn + 1}";
                headers.Add(headerText);
            }
            model.Headers = headers;

            for (int row = dataStartRow; row <= lastRow; row++)
            {
                var dataRow = new ExcelDataRow();
                dataRow.SiraNo = ws.Cell(row, firstColumn).GetValue<string>();
                dataRow.Soru = ws.Cell(row, firstColumn + 1).GetValue<string>();
                dataRow.Cevap = ws.Cell(row, firstColumn + 2).GetValue<string>();
                dataRow.Aciklama = ws.Cell(row, firstColumn + 3).GetValue<string>();
                dataRow.Sutun5 = ws.Cell(row, firstColumn + 4).GetValue<string>();
                dataRow.Sutun6 = ws.Cell(row, firstColumn + 5).GetValue<string>();
                excelData.Add(dataRow);
            }

            model.ExcelData = excelData;
            
            // Log başarılı sheet load
            var processingTime = (DateTime.Now - startTime).TotalSeconds;
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Sheet Load", User.Identity?.Name ?? "Unknown", selectedSheet, "Success", processingTime, $"{excelData.Count} satır yüklendi"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sayfa yüklenirken hata oluştu.");
            model.Message = "Sayfa verileri okunamadı.";
            
            // Log hatalı sheet load
            var processingTime = (DateTime.Now - startTime).TotalSeconds;
            _ = Task.Run(async () => await _adminService.LogActivityAsync("Sheet Load", User.Identity?.Name ?? "Unknown", selectedSheet, "Failed", processingTime, $"Hata: {ex.Message}"));
        }

        return View("Index", model);
    }

    [HttpPost]
    public IActionResult MarkSheetAsCompleted()
    {
        var selectedSheet = HttpContext.Session.GetString("SelectedSheet");
        if (string.IsNullOrWhiteSpace(selectedSheet))
        {
            return Json(new { success = false, message = "Sayfa seçilmedi." });
        }

        try
        {
            // Tamamlanan sayfaları session'dan al
            var completedSheetsJson = HttpContext.Session.GetString("CompletedSheets");
            var completedSheets = !string.IsNullOrEmpty(completedSheetsJson) 
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(completedSheetsJson) ?? new()
                : new List<string>();

            // Mevcut sayfayı tamamlanan listeye ekle
            if (!completedSheets.Contains(selectedSheet))
            {
                completedSheets.Add(selectedSheet);
            }

            // Session'a kaydet
            HttpContext.Session.SetString("CompletedSheets", System.Text.Json.JsonSerializer.Serialize(completedSheets));

            // Sonraki sayfa bilgisini al
            var sessionFilePath = HttpContext.Session.GetString("UploadedExcelPath");
            var nextSheetName = "";
            var isLastSheet = false;

            if (!string.IsNullOrWhiteSpace(sessionFilePath) && System.IO.File.Exists(sessionFilePath))
            {
                using var workbook = new XLWorkbook(sessionFilePath);
                var allSheets = workbook.Worksheets.Select(ws => ws.Name).ToList();
                var currentIndex = allSheets.IndexOf(selectedSheet);
                
                if (currentIndex < allSheets.Count - 1)
                {
                    nextSheetName = allSheets[currentIndex + 1];
                }
                else
                {
                    isLastSheet = true;
                }
            }

            return Json(new { 
                success = true, 
                completedCount = completedSheets.Count,
                nextSheetName = nextSheetName,
                isLastSheet = isLastSheet,
                message = isLastSheet ? "Tüm sayfalar tamamlandı!" : $"Sonraki sayfa: {nextSheetName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sayfa tamamlandı işaretlenirken hata oluştu");
            return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult ResetMultiPageProgress()
    {
        try
        {
            HttpContext.Session.Remove("CompletedSheets");
            HttpContext.Session.Remove("CurrentSheetIndex");
            return Json(new { success = true, message = "İlerleme sıfırlandı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İlerleme sıfırlanırken hata oluştu");
            return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult CloseChrome()
    {
        try
        {
            SeleniumService.QuitDriver();
            return Json(new { success = true, message = "Chrome tarayıcısı kapatıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chrome kapatılırken hata oluştu");
            return Json(new { success = false, message = "Chrome kapatılırken hata oluştu: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetProgress()
    {
        try
        {
            var completedSheetsJson = HttpContext.Session.GetString("CompletedSheets");
            var completedSheets = !string.IsNullOrEmpty(completedSheetsJson) 
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(completedSheetsJson) ?? new()
                : new List<string>();

            var uploadedExcelPath = HttpContext.Session.GetString("UploadedExcelPath");
            var totalSheets = 0;
            
            if (!string.IsNullOrEmpty(uploadedExcelPath) && System.IO.File.Exists(uploadedExcelPath))
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(uploadedExcelPath);
                totalSheets = workbook.Worksheets.Count;
            }

            return Json(new
            {
                success = true,
                completedCount = completedSheets.Count,
                totalSheets = totalSheets,
                completedSheets = completedSheets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Progress bilgisi alınırken hata oluştu");
            return Json(new { success = false, message = "Progress bilgisi alınamadı: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult DownloadExcelTemplate()
    {
        try
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ExcelSablonu.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Message"] = "Excel şablonu bulunamadı. Lütfen wwwroot klasörüne ExcelSablonu.xlsx dosyasını ekleyin.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = "MEB_Excel_Sablonu.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            
            return PhysicalFile(templatePath, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel şablonu indirilirken hata oluştu");
            TempData["Message"] = "Excel şablonu indirilemedi: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // FillWebsiteData metodu kaldırıldı - Selenium kullanılıyor
    /*[HttpPost]
    public IActionResult FillWebsiteData()
    {
        var sessionFilePath = HttpContext.Session.GetString("UploadedExcelPath");
        var selectedSheet = HttpContext.Session.GetString("SelectedSheet");
        
        if (string.IsNullOrWhiteSpace(sessionFilePath) || !System.IO.File.Exists(sessionFilePath))
        {
            return Json(new { success = false, message = "Excel dosyası bulunamadı." });
        }

        if (string.IsNullOrWhiteSpace(selectedSheet))
        {
            return Json(new { success = false, message = "Sayfa seçilmedi." });
        }

        try
        {
            // Excel verilerini oku
            var excelData = new Dictionary<int, string>();
            using var workbook = new XLWorkbook(sessionFilePath);
            var ws = workbook.Worksheet(selectedSheet);
            
            if (ws == null)
            {
                return Json(new { success = false, message = "Excel sayfası bulunamadı." });
            }

            var range = ws.RangeUsed();
            if (range == null)
            {
                return Json(new { success = false, message = "Excel sayfası boş." });
            }

            var firstRow = range.FirstRowUsed();
            var firstColumn = range.FirstColumnUsed().ColumnNumber();
            var lastColumn = range.LastColumnUsed().ColumnNumber();
            var headerRow = firstRow.RowNumber();
            var dataStartRow = headerRow + 1;
            var lastRow = range.LastRowUsed().RowNumber();

            // SN ve DOKÜMANTASYON KAYITLARI/UYGULAMALAR sütunlarını bul
            int snColumnIndex = -1;
            int answerColumnIndex = -1;
            
            for (int col = firstColumn; col <= lastColumn; col++)
            {
                var headerCell = ws.Cell(headerRow, col);
                var headerText = headerCell.GetString().Trim();
                
                if (headerText.Equals("SN", StringComparison.OrdinalIgnoreCase))
                {
                    snColumnIndex = col;
                }
                else if (headerText.Contains("DOKÜMANTASYON") || headerText.Contains("UYGULAMALAR"))
                {
                    answerColumnIndex = col;
                }
            }

            if (snColumnIndex == -1 || answerColumnIndex == -1)
            {
                return Json(new { success = false, message = "SN veya cevap sütunu bulunamadı." });
            }

            // Verileri oku
            for (int row = dataStartRow; row <= lastRow; row++)
            {
                var snCell = ws.Cell(row, snColumnIndex);
                var answerCell = ws.Cell(row, answerColumnIndex);
                
                if (int.TryParse(snCell.GetString(), out int questionNumber) && questionNumber > 0)
                {
                    var answer = answerCell.GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        excelData[questionNumber] = answer;
                    }
                }
            }

            // JavaScript kodu oluştur
            var jsCode = GenerateFillJavaScript(excelData, selectedSheet);
            
            return Json(new { success = true, jsCode = jsCode, dataCount = excelData.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Website veri doldurma hatası");
            return Json(new { success = false, message = "Veri işlenirken hata oluştu: " + ex.Message });
        }
    } */

    [HttpPost]
    public async Task<IActionResult> StartSeleniumFill()
    {
        var sessionFilePath = HttpContext.Session.GetString("UploadedExcelPath");
        var selectedSheet = HttpContext.Session.GetString("SelectedSheet");
        var username = HttpContext.Session.GetString("Username");

        if (string.IsNullOrWhiteSpace(sessionFilePath) || !System.IO.File.Exists(sessionFilePath))
        {
            return Json(new { success = false, message = "Önce bir Excel dosyası yükleyin." });
        }

        if (string.IsNullOrWhiteSpace(selectedSheet))
        {
            return Json(new { success = false, message = "Önce bir Excel sayfası seçin." });
        }

        try
        {
            var websiteUrl = "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum1.aspx";
            
            // SignalR ile real-time güncellemeler gönder
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ProgressHub>>();
            
            // İşlem başladığını bildir
            await hubContext.Clients.Group(username ?? "default").SendAsync("SeleniumProgress", 10, "Selenium işlemi başlatılıyor...", new List<string> { "Chrome tarayıcısı açılıyor..." });
            
            if (_seleniumService == null)
            {
                return Json(new { success = false, message = "Selenium servis kullanılamıyor." });
            }
            var result = await _seleniumService.FillWebsiteDataAsync(sessionFilePath, selectedSheet, websiteUrl);

            if (result.IsSuccess)
            {
                // Sayfa başarıyla doldurulduğunda otomatik olarak tamamlandı işaretle
                MarkSheetAsCompletedInternal(selectedSheet);
                
                // Tamamlandı bildirimi gönder
                await hubContext.Clients.Group(username ?? "default").SendAsync("SeleniumProgress", 100, "İşlem tamamlandı!", result.Logs);
                await hubContext.Clients.Group(username ?? "default").SendAsync("NotificationReceived", "success", $"Sayfa '{selectedSheet}' başarıyla dolduruldu!");
                
                return Json(new
                {
                    success = true,
                    status = result.Status,
                    processedCount = result.ProcessedCount,
                    totalCount = result.TotalCount,
                    logs = result.Logs,
                    message = $"Sayfa '{selectedSheet}' başarıyla dolduruldu!"
                });
            }
            else
            {
                // Hata bildirimi gönder
                await hubContext.Clients.Group(username ?? "default").SendAsync("SeleniumProgress", 0, "Hata oluştu!", result.Logs);
                await hubContext.Clients.Group(username ?? "default").SendAsync("NotificationReceived", "danger", "Selenium işlemi başarısız: " + result.ErrorMessage);
                
                return Json(new
                {
                    success = false,
                    errorMessage = result.ErrorMessage,
                    logs = result.Logs
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selenium işlemi sırasında hata oluştu");
            
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ProgressHub>>();
            var currentUsername = HttpContext.Session.GetString("Username");
            await hubContext.Clients.Group(currentUsername ?? "default").SendAsync("NotificationReceived", "danger", "Selenium işlemi sırasında hata oluştu: " + ex.Message);
            
            return Json(new { success = false, message = "Selenium işlemi sırasında hata oluştu: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult CancelSeleniumOperation()
    {
        try
        {
            var username = HttpContext.Session.GetString("Username");
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ProgressHub>>();
            
            // Selenium işlemini iptal et
            SeleniumService.QuitDriver();
            
            // İptal bildirimi gönder
            hubContext.Clients.Group(username ?? "default").SendAsync("SeleniumProgress", 0, "İşlem iptal edildi", new List<string> { "Kullanıcı tarafından iptal edildi" });
            hubContext.Clients.Group(username ?? "default").SendAsync("NotificationReceived", "warning", "Selenium işlemi iptal edildi.");
            
            return Json(new { success = true, message = "Selenium işlemi iptal edildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selenium işlemi iptal edilirken hata oluştu");
            return Json(new { success = false, message = "İptal işlemi başarısız: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetSystemStatus()
    {
        try
        {
            var status = new
            {
                cpu = GetCpuUsage(),
                memory = GetMemoryUsage(),
                uptime = GetSystemUptime(),
                activeUsers = GetActiveUsers(),
                timestamp = DateTime.Now
            };

            var username = HttpContext.Session.GetString("Username");
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ProgressHub>>();
            hubContext.Clients.Group(username ?? "default").SendAsync("SystemStatusUpdated", status);

            return Json(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sistem durumu alınırken hata oluştu");
            return Json(new { success = false, message = "Sistem durumu alınamadı: " + ex.Message });
        }
    }

    private double GetCpuUsage()
    {
        // Basit CPU kullanımı simülasyonu
        var random = new Random();
        return Math.Round(random.NextDouble() * 100, 1);
    }

    private double GetMemoryUsage()
    {
        // Basit memory kullanımı simülasyonu
        var random = new Random();
        return Math.Round(random.NextDouble() * 100, 1);
    }

    private TimeSpan GetSystemUptime()
    {
        return TimeSpan.FromHours(24); // Simüle edilmiş uptime
    }

    private int GetActiveUsers()
    {
        // Basit aktif kullanıcı sayısı simülasyonu
        var random = new Random();
        return random.Next(1, 10);
    }

    private bool MarkSheetAsCompletedInternal(string selectedSheet)
    {
        try
        {
            // Tamamlanan sayfaları session'dan al
            var completedSheetsJson = HttpContext.Session.GetString("CompletedSheets");
            var completedSheets = !string.IsNullOrEmpty(completedSheetsJson) 
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(completedSheetsJson) ?? new()
                : new List<string>();

            // Mevcut sayfayı tamamlanan listeye ekle
            if (!completedSheets.Contains(selectedSheet))
            {
                completedSheets.Add(selectedSheet);
            }

            // Session'a kaydet
            HttpContext.Session.SetString("CompletedSheets", System.Text.Json.JsonSerializer.Serialize(completedSheets));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sayfa tamamlandı işaretlenirken hata oluştu");
            return false;
        }
    }

    // GenerateFillJavaScript metodu kaldırıldı - Selenium kullanılıyor

    // GetSheetIndex ve GetIdRangeForPage metodları kaldırıldı - Selenium kullanılıyor
}
