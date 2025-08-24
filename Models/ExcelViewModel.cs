using System.Data;

namespace ExcelSheetsApp.Models
{
    public class ExcelViewModel
    {
        public string? FileName { get; set; }
        public List<string> SheetNames { get; set; } = new List<string>();
        public string? SelectedSheetName { get; set; }
        public DataTable? SheetData { get; set; }
        public string? ErrorMessage { get; set; }
    }
}


