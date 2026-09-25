@echo off
REM Quick script to run Flutter app on Windows Desktop (no emulator needed!)

echo ========================================
echo Flutter App - Run on Windows Desktop
echo ========================================
echo.
echo No Android SDK or emulator needed!
echo.

REM Check if Flutter is available
echo [1/5] Checking Flutter installation...
where flutter >nul 2>nul
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Flutter not found in PATH
    echo.
    echo Your Flutter SDK is at: C:\flutter
    echo.
    echo Please add C:\flutter\bin to your PATH:
    echo.
    echo PowerShell ^(as Admin^):
    echo [System.Environment]::SetEnvironmentVariable^("Path", [System.Environment]::GetEnvironmentVariable^("Path", "User"^) + ";C:\flutter\bin", "User"^)
    echo.
    echo Then restart this terminal and run this script again.
    echo.
    pause
    exit /b 1
)

flutter --version
echo.

REM Enable Windows desktop
echo [2/5] Enabling Windows desktop support...
flutter config --enable-windows-desktop
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

REM Generate code
echo [4/5] Generating code...
flutter pub run build_runner build --delete-conflicting-outputs
if %errorlevel% neq 0 (
    echo ERROR: Code generation failed
    pause
    exit /b 1
)
echo.

REM Check devices
echo [5/5] Checking available devices...
flutter devices
echo.

echo ========================================
echo Ready to run!
echo ========================================
echo.
echo The app will launch as a Windows desktop application.
echo.
echo Press any key to start the app...
pause >nul

echo.
echo Starting Flutter app on Windows...
echo.
echo Hot reload: Press 'r' in the terminal
echo Hot restart: Press 'R' in the terminal
echo Quit: Press 'q' in the terminal
echo.

flutter run -d windows
