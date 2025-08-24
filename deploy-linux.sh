#!/bin/bash

# Linux Deployment Script
echo "🚀 ExcelSheetsApp Deployment Script"

# Stop existing processes
echo "Stopping existing application..."
pkill -f "ExcelSheetsApp" || true

# Backup existing files
BACKUP_DIR="backup-$(date +%Y%m%d-%H%M%S)"
if [ -d "publish" ]; then
    echo "Creating backup..."
    mv publish $BACKUP_DIR
fi

# Build and publish
echo "Building application..."
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o publish

# Set permissions
chmod +x publish/ExcelSheetsApp

# Run database migrations
echo "Running database migrations..."
cd publish
dotnet ef database update --no-build

# Create systemd service (optional)
read -p "Do you want to create a systemd service? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    sudo tee /etc/systemd/system/excelsheets.service > /dev/null <<EOF
[Unit]
Description=ExcelSheetsApp
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=$(pwd)
ExecStart=/usr/bin/dotnet ExcelSheetsApp.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

    sudo systemctl daemon-reload
    sudo systemctl enable excelsheets
    sudo systemctl start excelsheets
    echo "✅ Systemd service created and started"
else
    # Start manually
    echo "Starting application manually..."
    nohup dotnet ExcelSheetsApp.dll > app.log 2>&1 &
fi

echo "✅ Deployment completed successfully!"
echo "Application is running at: http://localhost:5000"
