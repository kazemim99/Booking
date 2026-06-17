# Booksy - Stop All Services
# This script stops all running services

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Booksy - Stopping All Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Stop .NET processes
Write-Host "Stopping backend services..." -ForegroundColor Yellow

$processes = @(
    "*Booksy.UserManagement.API*",
    "*Booksy.ServiceCatalog.Api*",
    "*Booksy.Gateway*"
)

foreach ($processPattern in $processes) {
    $procs = Get-Process | Where-Object { $_.ProcessName -like $processPattern -or $_.Path -like $processPattern }
    if ($procs) {
        foreach ($proc in $procs) {
            Write-Host "Stopping $($proc.ProcessName) (PID: $($proc.Id))..." -ForegroundColor Green
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

# Stop Node processes (Vite dev server)
Write-Host "Stopping frontend..." -ForegroundColor Yellow
$nodeProcs = Get-Process node -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*vite*" -or $_.CommandLine -like "*booksy-frontend*"
}

if ($nodeProcs) {
    foreach ($proc in $nodeProcs) {
        Write-Host "Stopping Node.js process (PID: $($proc.Id))..." -ForegroundColor Green
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "All services stopped successfully!" -ForegroundColor Green
Write-Host ""
