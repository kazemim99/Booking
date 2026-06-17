# Run this script as Administrator to allow Flutter app to connect to backend

# Add firewall rules for Booksy backend ports
Write-Host "Adding firewall rules for Booksy backend..." -ForegroundColor Green

# Check if rule already exists
$existingRule = Get-NetFirewallRule -DisplayName "Booksy Backend Ports" -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-Host "Firewall rule already exists. Removing old rule..." -ForegroundColor Yellow
    Remove-NetFirewallRule -DisplayName "Booksy Backend Ports"
}

# Add new rule
New-NetFirewallRule -DisplayName "Booksy Backend Ports" `
    -Direction Inbound `
    -LocalPort 5000,5001,5002 `
    -Protocol TCP `
    -Action Allow `
    -Profile Private,Public `
    -Description "Allow Flutter app to connect to Booksy backend services (Gateway:5000, UserManagement:5001, ServiceCatalog:5002)"

Write-Host "✅ Firewall rules added successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Ports allowed:" -ForegroundColor Cyan
Write-Host "  - 5000 (Gateway)" -ForegroundColor White
Write-Host "  - 5001 (UserManagement API)" -ForegroundColor White
Write-Host "  - 5002 (ServiceCatalog API)" -ForegroundColor White
Write-Host ""
Write-Host "Your PC IP: 172.20.105.136" -ForegroundColor Cyan
Write-Host "Flutter app will connect to: http://172.20.105.136:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Make sure all backend services are running in Visual Studio (http profile)" -ForegroundColor White
Write-Host "2. Run Flutter app with: flutter run" -ForegroundColor White
Write-Host "3. The app should now connect successfully!" -ForegroundColor White
