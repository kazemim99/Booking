# Add Booksy scripts to Windows PATH
# Run this script as Administrator

$booksyPath = $PSScriptRoot

Write-Host "Adding '$booksyPath' to system PATH..." -ForegroundColor Cyan

# Get current PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

# Check if already in PATH
if ($currentPath -split ';' | Where-Object { $_ -eq $booksyPath }) {
    Write-Host "Path already exists in PATH!" -ForegroundColor Yellow
    exit 0
}

# Add to PATH
$newPath = "$currentPath;$booksyPath"
[Environment]::SetEnvironmentVariable("Path", $newPath, "User")

Write-Host "Successfully added to PATH!" -ForegroundColor Green
Write-Host ""
Write-Host "Please restart your terminal for changes to take effect." -ForegroundColor Yellow
Write-Host "After restart, you can run 'run-all.ps1' from any directory." -ForegroundColor Cyan
