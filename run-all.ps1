# Booksy - Run All Services (Development)
# This script starts all backend APIs and the frontend application

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Booksy - Starting All Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to start a process in a new window
function Start-ServiceInNewWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [string]$Command,
        [string]$Arguments
    )

    Write-Host "Starting $Title..." -ForegroundColor Green
    Start-Process -FilePath $Command -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -WindowStyle Normal
    Start-Sleep -Seconds 2
}

# Check if .NET SDK is installed
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET SDK first." -ForegroundColor Red
    exit 1
}

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✓ Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Node.js not found. Please install Node.js first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting backend services..." -ForegroundColor Yellow

# Start UserManagement API (Port 5001)
Start-ServiceInNewWindow `
    -Title "UserManagement API" `
    -WorkingDirectory "$PSScriptRoot\src\UserManagement\Booksy.UserManagement.API" `
    -Command "powershell" `
    -Arguments "-NoExit -Command `"Write-Host 'UserManagement API - Port 5001' -ForegroundColor Cyan; dotnet run --project Booksy.UserManagement.API.csproj`""

# Start ServiceCatalog API (Port 5002)
Start-ServiceInNewWindow `
    -Title "ServiceCatalog API" `
    -WorkingDirectory "$PSScriptRoot\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api" `
    -Command "powershell" `
    -Arguments "-NoExit -Command `"Write-Host 'ServiceCatalog API - Port 5002' -ForegroundColor Cyan; dotnet run --project Booksy.ServiceCatalog.Api.csproj`""

# Start Gateway (Port 5000)
Start-ServiceInNewWindow `
    -Title "API Gateway" `
    -WorkingDirectory "$PSScriptRoot\src\APIGateway\Booksy.Gateway" `
    -Command "powershell" `
    -Arguments "-NoExit -Command `"Write-Host 'API Gateway - Port 5000' -ForegroundColor Cyan; dotnet run --project Booksy.Gateway.csproj`""

# Wait for backend services to start
Write-Host ""
Write-Host "Waiting for backend services to initialize (15 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check if npm install is needed for frontend
$frontendPath = "$PSScriptRoot\booksy-frontend"
if (!(Test-Path "$frontendPath\node_modules")) {
    Write-Host ""
    Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
    Set-Location $frontendPath
    npm install
    Set-Location $PSScriptRoot
}

# Start Frontend
Write-Host ""
Write-Host "Starting frontend application..." -ForegroundColor Yellow
Start-ServiceInNewWindow `
    -Title "Booksy Frontend" `
    -WorkingDirectory $frontendPath `
    -Command "powershell" `
    -Arguments "-NoExit -Command `"Write-Host 'Frontend - Vite Dev Server' -ForegroundColor Cyan; npm run dev`""

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  All Services Started Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Cyan
Write-Host "  - UserManagement API:  http://localhost:5001" -ForegroundColor White
Write-Host "  - ServiceCatalog API:  http://localhost:5002" -ForegroundColor White
Write-Host "  - API Gateway:         http://localhost:5000" -ForegroundColor White
Write-Host "  - Frontend:            http://localhost:5173" -ForegroundColor White
Write-Host ""
Write-Host "Swagger Documentation:" -ForegroundColor Cyan
Write-Host "  - UserManagement:      http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  - ServiceCatalog:      http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  - Gateway:             http://localhost:5000/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to close this window (services will continue running)..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
