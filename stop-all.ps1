# Booksy - Stop All Dev Services
# Kills the process trees for windows started by run-all.ps1: the backend
# (Booksy.Host), the two Flutter web apps, the admin panel, and the legacy
# customer web frontend. Matches on the launcher's own command line rather
# than bare process names, since dotnet.exe/node.exe/flutter are ambiguous
# on their own.

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Booksy - Stopping Dev Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Stop-ProcessTree {
    param([int]$ParentId)
    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ParentId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        Stop-ProcessTree -ParentId $child.ProcessId
    }
    Stop-Process -Id $ParentId -Force -ErrorAction SilentlyContinue
}

$patterns = @(
    @{ Name = "Backend (Booksy.Host)";     Regex = 'Booksy\.Host\.csproj' },
    @{ Name = "Customer App (Flutter)";    Regex = '[\\/]booksy-customer-app' },
    @{ Name = "Provider App (Flutter)";    Regex = '[\\/]booksy-provider-app' },
    @{ Name = "Admin Panel (Vite)";        Regex = '[\\/]booksy-admin' },
    @{ Name = "Customer Web (Vite)";       Regex = '[\\/]booksy-frontend' }
)

$shells = Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" -ErrorAction SilentlyContinue

$stoppedAny = $false
foreach ($pattern in $patterns) {
    $matches = $shells | Where-Object { $_.CommandLine -match $pattern.Regex }
    foreach ($m in $matches) {
        Write-Host "Stopping $($pattern.Name) (PID $($m.ProcessId))..." -ForegroundColor Green
        Stop-ProcessTree -ParentId $m.ProcessId
        $stoppedAny = $true
    }
}

if (-not $stoppedAny) {
    Write-Host "No matching dev-service windows found." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Note: infrastructure containers (Postgres/Redis/Seq/pgAdmin) are not stopped by this script." -ForegroundColor Yellow
Write-Host "Run this to stop them too:" -ForegroundColor Yellow
Write-Host "  docker-compose -f docker-compose.infrastructure.yml down" -ForegroundColor White
Write-Host ""
Write-Host "Done." -ForegroundColor Green
