using System.ComponentModel.DataAnnotations;

namespace ExcelSheetsApp.Models
{
    // Admin Dashboard Model
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public string SystemStatus { get; set; } = string.Empty;
        
        // Yeni istatistikler
        public int TotalExcelFilesProcessed { get; set; }
        public int TodayOperations { get; set; }
        public int ThisWeekOperations { get; set; }
        public int ThisMonthOperations { get; set; }
        public double AverageProcessingTime { get; set; }
        public List<ActivityLogEntry> RecentActivities { get; set; } = new List<ActivityLogEntry>();
        public List<PopularFileType> PopularFileTypes { get; set; } = new List<PopularFileType>();
        public SystemPerformance Performance { get; set; } = new SystemPerformance();
    }

    // Aktivite Log Girdisi
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    // Popüler Dosya Türü
    public class PopularFileType
    {
        public string Extension { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    // Sistem Performansı
    public class SystemPerformance
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    // Kullanıcı Yönetimi Model
    public class UserViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir!")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir!")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rol gereklidir!")]
        [Display(Name = "Rol")]
        public string Role { get; set; } = "User";

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    // Sistem Ayarları Model
    public class SystemSettingsViewModel
    {
        [Display(Name = "Selenium Timeout (saniye)")]
        [Range(10, 120, ErrorMessage = "Timeout 10-120 saniye arasında olmalıdır")]
        public int SeleniumTimeout { get; set; } = 30;

        [Display(Name = "Maksimum Dosya Boyutu (MB)")]
        [Range(1, 50, ErrorMessage = "Dosya boyutu 1-50 MB arasında olmalıdır")]
        public int MaxFileSize { get; set; } = 10;

        [Display(Name = "İzin Verilen Dosya Türleri")]
        public string AllowedFileTypes { get; set; } = ".xlsx,.xls";

        [Display(Name = "Tarayıcıyı Otomatik Kapat")]
        public bool AutoCloseBrowser { get; set; } = false;
    }

    // İstatistikler Model
    public class SystemStatisticsViewModel
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double AverageProcessingTime { get; set; }
        public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
    }

    // Log Girişi Model
    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
