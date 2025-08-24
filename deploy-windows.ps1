# Windows Deployment Script
Write-Host "🚀 ExcelSheetsApp Deployment Script" -ForegroundColor Green

# Stop existing processes
Write-Host "Stopping existing application..." -ForegroundColor Yellow
Get-Process -Name "ExcelSheetsApp" -ErrorAction SilentlyContinue | Stop-Process -Force

# Backup existing files
$backupDir = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (Test-Path "publish") {
    Write-Host "Creating backup..." -ForegroundColor Yellow
    Move-Item "publish" $backupDir
}

# Build and publish
Write-Host "Building application..." -ForegroundColor Yellow
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o publish

# Run database migrations
Write-Host "Running database migrations..." -ForegroundColor Yellow
Set-Location publish
dotnet ef database update --no-build

# Start application
Write-Host "Starting application..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "ExcelSheetsApp.dll" -WindowStyle Hidden

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
Write-Host "Application is running at: http://localhost:5000" -ForegroundColor Cyan
