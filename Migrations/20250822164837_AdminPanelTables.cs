using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelSheetsApp.Migrations
{
    /// <inheritdoc />
    public partial class AdminPanelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProcessingTimeSeconds = table.Column<double>(type: "REAL", nullable: true),
                    FileExtension = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalOperations = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulOperations = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedOperations = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageProcessingTime = table.Column<double>(type: "REAL", nullable: false),
                    UniqueUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDataProcessed = table.Column<long>(type: "INTEGER", nullable: false),
                    ExcelFilesProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    SheetsProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsProcessed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileTypeStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTypeStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Exception = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Action",
                table: "ActivityLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Username",
                table: "ActivityLogs",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_DailyStatistics_Date",
                table: "DailyStatistics",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileTypeStatistics_Extension",
                table: "FileTypeStatistics",
                column: "Extension",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Level",
                table: "SystemLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Timestamp",
                table: "SystemLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "DailyStatistics");

            migrationBuilder.DropTable(
                name: "FileTypeStatistics");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
