using Microsoft.AspNetCore.Mvc;
using ExcelSheetsApp.Models;
using ExcelSheetsApp.Services;
using System.Data;

namespace ExcelSheetsApp.Controllers
{
    public class ExcelController : Controller
    {
        private readonly IExcelService _excelService;
        private static string? _currentFilePath;

        public ExcelController(IExcelService excelService)
        {
            _excelService = excelService;
        }

        public IActionResult Index()
        {
            return View(new ExcelViewModel());
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Lütfen bir dosya seçin." });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return Json(new { success = false, message = "Lütfen geçerli bir Excel dosyası seçin." });
            }

            try
            {
                // Dosyayı geçici olarak kaydet
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                _currentFilePath = filePath;

                // Excel sayfalarını al
                var model = _excelService.GetSheetNames(filePath);
                
                if (!string.IsNullOrEmpty(model.ErrorMessage))
                {
                    return Json(new { success = false, message = model.ErrorMessage });
                }

                return Json(new { 
                    success = true, 
                    fileName = model.FileName,
                    sheetNames = model.SheetNames 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Dosya yüklenirken hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult GetSheetData(string sheetName)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return Json(new { success = false, message = "Önce bir Excel dosyası yükleyin." });
            }

            try
            {
                var dataTable = _excelService.GetSheetData(_currentFilePath, sheetName);
                
                // DataTable'ı JSON'a çevir
                var data = new List<Dictionary<string, object>>();
                var columns = new List<string>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    columns.Add(column.ColumnName);
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    var rowData = new Dictionary<string, object>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        rowData[columns[i]] = row[i]?.ToString() ?? "";
                    }
                    data.Add(rowData);
                }

                return Json(new { 
                    success = true, 
                    columns = columns,
                    data = data 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Sayfa verisi alınırken hata oluştu: {ex.Message}" });
            }
        }
    }
}
