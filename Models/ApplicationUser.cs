using Microsoft.AspNetCore.Identity;

namespace ExcelSheetsApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
