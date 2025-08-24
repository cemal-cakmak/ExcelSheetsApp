using ClosedXML.Excel;
using System.Data;
using ExcelSheetsApp.Models;

namespace ExcelSheetsApp.Services
{
    public interface IExcelService
    {
        ExcelViewModel GetSheetNames(string filePath);
        DataTable GetSheetData(string filePath, string sheetName);
    }

    public class ExcelService : IExcelService
    {
        public ExcelViewModel GetSheetNames(string filePath)
        {
            var model = new ExcelViewModel();
            
            try
            {
                using var workbook = new XLWorkbook(filePath);
                model.FileName = Path.GetFileName(filePath);
                model.SheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Excel dosyası okunurken hata oluştu: {ex.Message}";
            }
            
            return model;
        }

        public DataTable GetSheetData(string filePath, string sheetName)
        {
            var dataTable = new DataTable();
            
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(sheetName);
                
                if (worksheet == null)
                    return dataTable;

                var range = worksheet.RangeUsed();
                if (range == null)
                    return dataTable;

                // Başlık satırını al
                var headerRow = range.FirstRow();
                foreach (var cell in headerRow.Cells())
                {
                    dataTable.Columns.Add(cell.Value.ToString() ?? $"Column{dataTable.Columns.Count + 1}");
                }

                // Veri satırlarını al
                var dataRows = range.Rows().Skip(1); // İlk satır başlık olduğu için atla
                foreach (var row in dataRows)
                {
                    var dataRow = dataTable.NewRow();
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        dataRow[i] = row.Cell(i + 1).Value.ToString() ?? "";
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda boş DataTable döndür
                Console.WriteLine($"Excel sayfası okunurken hata: {ex.Message}");
            }
            
            return dataTable;
        }
    }
}
