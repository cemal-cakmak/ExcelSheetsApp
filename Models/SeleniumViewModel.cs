namespace ExcelSheetsApp.Models;

public class SeleniumViewModel
{
    public string? WebsiteUrl { get; set; }
    public bool IsProcessing { get; set; }
    public string? Status { get; set; }
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Logs { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
