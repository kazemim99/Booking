@echo off
REM AsanRezerve Launcher Batch File
REM This allows you to run 'asan-rezerve start' or 'asan-rezerve stop' from anywhere

if "%1"=="start" (
    powershell -ExecutionPolicy Bypass -File "c:\Repos\Booking\run-all.ps1"
) else if "%1"=="stop" (
    powershell -ExecutionPolicy Bypass -File "c:\Repos\Booking\stop-all.ps1"
) else (
    echo Usage:
    echo   asan-rezerve start  - Start all services
    echo   asan-rezerve stop   - Stop all services
)
