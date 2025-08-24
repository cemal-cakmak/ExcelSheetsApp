using System.ComponentModel.DataAnnotations;

namespace ExcelSheetsApp.Models;

public class HomeViewModel
{
    public string? UploadedFilePath { get; set; }
    public List<string> Sheets { get; set; } = new List<string>();
    public string? SelectedSheet { get; set; }
    public List<ExcelDataRow> ExcelData { get; set; } = new List<ExcelDataRow>();
    public string? Message { get; set; }
    
    // Multi-page progress properties
    public int TotalSheets { get; set; }
    public List<string> CompletedSheets { get; set; } = new List<string>();
    public int CurrentSheetIndex { get; set; }
    
    // Excel header information
    public List<string> Headers { get; set; } = new List<string>();
}

public class ExcelDataRow
{
    public string SiraNo { get; set; } = "";
    public string Soru { get; set; } = "";
    public string Cevap { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Sutun5 { get; set; } = "";
    public string Sutun6 { get; set; } = "";
}
