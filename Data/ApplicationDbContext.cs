using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ExcelSheetsApp.Models;

namespace ExcelSheetsApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Admin Panel Tabloları
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<DailyStatistic> DailyStatistics { get; set; }
    public DbSet<FileTypeStatistic> FileTypeStatistics { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // ActivityLog konfigürasyonu
        builder.Entity<ActivityLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Username);
            entity.HasIndex(e => e.Action);
        });
        
        // SystemSetting konfigürasyonu
        builder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });
        
        // DailyStatistic konfigürasyonu
        builder.Entity<DailyStatistic>(entity =>
        {
            entity.HasIndex(e => e.Date).IsUnique();
        });
        
        // FileTypeStatistic konfigürasyonu
        builder.Entity<FileTypeStatistic>(entity =>
        {
            entity.HasIndex(e => e.Extension).IsUnique();
        });
        
        // SystemLog konfigürasyonu
        builder.Entity<SystemLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
        });
    }
}
