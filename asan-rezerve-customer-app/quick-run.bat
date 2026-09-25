@echo off
REM Simple script to run Flutter app on Windows

echo Checking Flutter...
flutter --version
if errorlevel 1 (
    echo.
    echo ERROR: Flutter not found!
    echo Add C:\flutter\bin to your PATH
    pause
    exit /b 1
)

echo.
echo Installing packages...
flutter pub get

echo.
echo Generating code...
flutter pub run build_runner build --delete-conflicting-outputs

echo.
echo Running app on Windows...
flutter run -d windows
