# Flutter Customer App - Quick Setup Script for Windows (PowerShell)
# Run with: powershell -ExecutionPolicy Bypass -File setup.ps1

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Booksy Customer App - Quick Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if Flutter is installed
Write-Host "[1/5] Checking Flutter installation..." -ForegroundColor Yellow

$flutterInstalled = Get-Command flutter -ErrorAction SilentlyContinue
if (-not $flutterInstalled) {
    Write-Host ""
    Write-Host "ERROR: Flutter is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please follow these steps:" -ForegroundColor Yellow
    Write-Host "1. Download Flutter SDK from: https://docs.flutter.dev/get-started/install/windows"
    Write-Host "2. Extract to C:\flutter"
    Write-Host "3. Add C:\flutter\bin to your PATH"
    Write-Host "4. Restart this terminal"
    Write-Host ""
    Write-Host "Quick PATH setup (run in PowerShell as Administrator):" -ForegroundColor Cyan
    Write-Host '[System.Environment]::SetEnvironmentVariable("Path", [System.Environment]::GetEnvironmentVariable("Path", "User") + ";C:\flutter\bin", "User")'
    Write-Host ""
    Write-Host "For detailed instructions, see WINDOWS_SETUP.md"
    Read-Host "Press Enter to exit"
    exit 1
}

flutter --version
Write-Host ""

# Run Flutter Doctor
Write-Host "[2/5] Running Flutter Doctor..." -ForegroundColor Yellow
flutter doctor
Write-Host ""

# Install dependencies
Write-Host "[3/5] Installing Flutter packages..." -ForegroundColor Yellow
flutter pub get
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to install packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host ""

# Run code generation
Write-Host "[4/5] Generating code (JSON serialization, Retrofit, Injectable)..." -ForegroundColor Yellow
flutter pub run build_runner build --delete-conflicting-outputs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Code generation failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host ""

# Check available devices
Write-Host "[5/5] Checking available devices..." -ForegroundColor Yellow
flutter devices
Write-Host ""

Write-Host "====================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Start your Android emulator or connect a device"
Write-Host "2. Run: flutter run"
Write-Host "3. Or run in release mode: flutter run --release"
Write-Host ""
Write-Host "For detailed instructions, see WINDOWS_SETUP.md"
Write-Host ""
Read-Host "Press Enter to exit"
