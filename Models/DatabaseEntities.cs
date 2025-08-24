using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelSheetsApp.Models
{
    // Aktivite Log Tablosu - Ana sayfadaki işlemleri izler
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // "Excel Upload", "Sheet Load", "Selenium Process"
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty; // "Success", "Failed", "Processing"
        
        [MaxLength(500)]
        public string Details { get; set; } = string.Empty; // Ek bilgiler
        
        public double? ProcessingTimeSeconds { get; set; }
        
        [MaxLength(50)]
        public string FileExtension { get; set; } = string.Empty;
        
        public long? FileSizeBytes { get; set; }
    }
    
    // Sistem Ayarları Tablosu
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        [MaxLength(50)]
        public string ModifiedBy { get; set; } = string.Empty;
    }
    
    // Günlük İstatistikler Tablosu
    public class DailyStatistic
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double AverageProcessingTime { get; set; }
        public int UniqueUsers { get; set; }
        public long TotalDataProcessed { get; set; } // bytes
        
        // Excel specific stats
        public int ExcelFilesProcessed { get; set; }
        public int SheetsProcessed { get; set; }
        public int RowsProcessed { get; set; }
    }
    
    // Dosya Türü İstatistikleri
    public class FileTypeStatistic
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;
        
        public int Count { get; set; }
        public long TotalSizeBytes { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
    
    // Sistem Log Tablosu
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = string.Empty; // Info, Warning, Error, Debug
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Source { get; set; } = string.Empty; // Controller/Action name
        
        [MaxLength(2000)]
        public string Exception { get; set; } = string.Empty;
        
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string UserAgent { get; set; } = string.Empty;
    }
}
