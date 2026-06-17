@echo off
REM Flutter Customer App - Quick Setup Script for Windows
REM This script helps automate the setup process

echo ====================================
echo Booksy Customer App - Quick Setup
echo ====================================
echo.

REM Check if Flutter is installed
echo [1/5] Checking Flutter installation...
where flutter >nul 2>nul
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Flutter is not installed or not in PATH
    echo.
    echo Please follow these steps:
    echo 1. Download Flutter SDK from: https://docs.flutter.dev/get-started/install/windows
    echo 2. Extract to C:\flutter
    echo 3. Add C:\flutter\bin to your PATH
    echo 4. Restart this terminal
    echo.
    echo For detailed instructions, see WINDOWS_SETUP.md
    pause
    exit /b 1
)

flutter --version
echo.

REM Run Flutter Doctor
echo [2/5] Running Flutter Doctor...
flutter doctor
echo.

REM Install dependencies
echo [3/5] Installing Flutter packages...
flutter pub get
if %errorlevel% neq 0 (
    echo ERROR: Failed to install packages
    pause
    exit /b 1
)
echo.

REM Run code generation
echo [4/5] Generating code (JSON serialization, Retrofit, Injectable)...
flutter pub run build_runner build --delete-conflicting-outputs
if %errorlevel% neq 0 (
    echo ERROR: Code generation failed
    pause
    exit /b 1
)
echo.

REM Check available devices
echo [5/5] Checking available devices...
flutter devices
echo.

echo ====================================
echo Setup Complete!
echo ====================================
echo.
echo Next steps:
echo 1. Start your Android emulator or connect a device
echo 2. Run: flutter run
echo 3. Or run in release mode: flutter run --release
echo.
echo For detailed instructions, see WINDOWS_SETUP.md
echo.
pause
