// Railway test için ultra basit program
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("🚀 BAŞLIYOR...");

var app = builder.Build();

// Basit endpoint
app.MapGet("/", () => "ExcelSheetsApp çalışıyor! 🎉");
app.MapGet("/health", () => "OK");

// Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Port: {port}");

app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine("✅ BAŞLATILIYOR...");
app.Run();
