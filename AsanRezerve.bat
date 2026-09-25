@echo off
REM Booksy Launcher Batch File
REM This allows you to run 'booksy start' or 'booksy stop' from anywhere

if "%1"=="start" (
    powershell -ExecutionPolicy Bypass -File "c:\Repos\Booking\run-all.ps1"
) else if "%1"=="stop" (
    powershell -ExecutionPolicy Bypass -File "c:\Repos\Booking\stop-all.ps1"
) else (
    echo Usage:
    echo   booksy start  - Start all services
    echo   booksy stop   - Stop all services
)
