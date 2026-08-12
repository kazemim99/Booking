# Booksy - Run Dev Stack (selectable)
# Launches any combination of: infra (docker), backend (Booksy.Host), the two
# Flutter apps, the admin panel, and the legacy customer web frontend.
#
# Usage:
#   .\run-all.ps1                          # interactive menu
#   .\run-all.ps1 -All                     # everything
#   .\run-all.ps1 -Backend -Admin          # just these two
#   .\run-all.ps1 -Infra -Backend -Customer -Provider -Admin -Frontend

param(
    [switch]$Infra,
    [switch]$Backend,
    [switch]$Customer,
    [switch]$Provider,
    [switch]$Admin,
    [switch]$Frontend,
    [switch]$All,
    # Pins every OTP code the backend generates to this fixed value (see
    # OtpCode.Generate in the UserManagement domain) so phone-verification
    # flows can be exercised manually without digging the real code out of
    # the backend log. Never respected in production - only read when the
    # env var is set, which this script only does for local runs.
    [string]$OtpSandboxCode = "123456",
    # Skip pinning the OTP code and let the backend generate real random
    # codes (find them in the backend window's log / Seq instead).
    [switch]$NoOtpSandbox
)

$repoRoot = $PSScriptRoot

function Start-InNewWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [string]$Command,
        [string]$BackgroundColor = "Black",
        [string]$ForegroundColor = "White"
    )
    # Set colors and Clear-Host before anything else so the window is fully
    # repainted from the moment it opens, rather than staying black until the
    # first scroll past the initial (pre-color) buffer contents.
    $inner = "`$host.ui.RawUI.BackgroundColor = '$BackgroundColor'; " +
             "`$host.ui.RawUI.ForegroundColor = '$ForegroundColor'; " +
             "Clear-Host; " +
             "`$host.ui.RawUI.WindowTitle = '$Title'; " +
             "Set-Location -LiteralPath '$WorkingDirectory'; $Command"
    Start-Process -FilePath "powershell.exe" -ArgumentList @('-NoExit', '-Command', $inner) | Out-Null
    Write-Host "Started: $Title" -ForegroundColor Green
}

function Test-CommandExists {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

$serviceLabels = [ordered]@{
    Infra    = "Infrastructure   (Postgres/Redis/Seq/pgAdmin - Docker)"
    Backend  = "Backend API      (Booksy.Host, :5000)"
    Customer = "Customer App     (Flutter, web)"
    Provider = "Provider App     (Flutter, web)"
    Admin    = "Admin Panel      (Vue, Vite)"
    Frontend = "Customer Web     (booksy-frontend, legacy Vue site)"
}

# One background/foreground pair per service window, so you can tell them
# apart at a glance in the taskbar / Alt-Tab without reading titles.
$serviceColors = @{
    Backend  = @{ Bg = "DarkBlue";    Fg = "White" }
    Customer = @{ Bg = "DarkGreen";   Fg = "White" }
    Provider = @{ Bg = "DarkMagenta"; Fg = "White" }
    Admin    = @{ Bg = "DarkCyan";    Fg = "Black" }
    Frontend = @{ Bg = "DarkRed";     Fg = "White" }
}

# Interactive checkbox list: Up/Down to move, Space to toggle, A to toggle all,
# Enter to confirm, Esc to cancel. Falls back to a numbered prompt on hosts that
# cannot do raw key reads (ISE, redirected stdin/stdout, CI).
function Select-Services {
    param(
        [Parameter(Mandatory)][string[]]$Keys,
        [Parameter(Mandatory)][string[]]$Labels,
        [string[]]$PreChecked = @()
    )

    $count = $Keys.Count
    $checked = New-Object 'bool[]' $count
    for ($i = 0; $i -lt $count; $i++) {
        if ($PreChecked -contains $Keys[$i]) { $checked[$i] = $true }
    }

    $canInteract = $true
    if ($Host.Name -match 'ISE') { $canInteract = $false }
    try {
        if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected) { $canInteract = $false }
    } catch { $canInteract = $false }

    if (-not $canInteract) {
        Write-Host ""
        Write-Host "  Booksy Dev Launcher" -ForegroundColor Cyan
        for ($i = 0; $i -lt $count; $i++) {
            Write-Host ("  {0}) {1}" -f ($i + 1), $Labels[$i])
        }
        Write-Host ""
        $raw = Read-Host "Enter numbers separated by commas (e.g. 2,3,5), or 'a' for all"
        if ($raw -match '^\s*a\s*$') { return $Keys }
        $picked = $raw -split ',' | ForEach-Object { $_.Trim() }
        $result = @()
        for ($i = 0; $i -lt $count; $i++) {
            if ($picked -contains [string]($i + 1)) { $result += $Keys[$i] }
        }
        return $result
    }

    $header = @(
        "",
        "  Booksy Dev Launcher - select what to start",
        "  Up/Down move    Space toggle    A all    Enter run    Esc cancel",
        ""
    )
    $blockHeight = $header.Count + $count + 1

    # Print the block once so any buffer scrolling happens before we cache the
    # anchor row; otherwise the anchor drifts and the redraw corrupts the screen.
    for ($i = 0; $i -lt $blockHeight; $i++) { Write-Host "" }
    $anchor = [Console]::CursorTop - $blockHeight

    $index = 0
    $cancelled = $false
    $prevCursorVisible = $true
    try { $prevCursorVisible = [Console]::CursorVisible } catch { }
    try { [Console]::CursorVisible = $false } catch { }

    try {
        while ($true) {
            $width = [Math]::Max(40, [Console]::WindowWidth - 1)
            [Console]::SetCursorPosition(0, $anchor)

            foreach ($line in $header) {
                $text = $line
                if ($text.Length -gt $width) { $text = $text.Substring(0, $width) }
                Write-Host $text.PadRight($width)
            }

            for ($i = 0; $i -lt $count; $i++) {
                $mark = if ($checked[$i]) { "[x]" } else { "[ ]" }
                $arrow = if ($i -eq $index) { ">" } else { " " }
                $text = "   $arrow $mark $($Labels[$i])"
                if ($text.Length -gt $width) { $text = $text.Substring(0, $width) }
                if ($i -eq $index) {
                    Write-Host $text.PadRight($width) -ForegroundColor Cyan
                } elseif ($checked[$i]) {
                    Write-Host $text.PadRight($width) -ForegroundColor Green
                } else {
                    Write-Host $text.PadRight($width)
                }
            }
            Write-Host "".PadRight($width)

            $key = [Console]::ReadKey($true)

            if ($key.Key -eq 'Enter') {
                break
            } elseif ($key.Key -eq 'Escape') {
                $cancelled = $true
                break
            } elseif ($key.Key -eq 'UpArrow') {
                $index = ($index - 1 + $count) % $count
            } elseif ($key.Key -eq 'DownArrow') {
                $index = ($index + 1) % $count
            } elseif ($key.Key -eq 'Spacebar') {
                $checked[$index] = -not $checked[$index]
            } elseif ($key.KeyChar -eq 'a' -or $key.KeyChar -eq 'A') {
                # If everything is already on, A clears; otherwise it selects all.
                $allOn = -not ($checked -contains $false)
                for ($i = 0; $i -lt $count; $i++) { $checked[$i] = -not $allOn }
            }
        }
    } finally {
        try { [Console]::SetCursorPosition(0, $anchor + $blockHeight) } catch { }
        try { [Console]::CursorVisible = $prevCursorVisible } catch { }
    }

    if ($cancelled) { return $null }

    $result = @()
    for ($i = 0; $i -lt $count; $i++) {
        if ($checked[$i]) { $result += $Keys[$i] }
    }
    return $result
}

$selected = [ordered]@{
    Infra    = $Infra.IsPresent
    Backend  = $Backend.IsPresent
    Customer = $Customer.IsPresent
    Provider = $Provider.IsPresent
    Admin    = $Admin.IsPresent
    Frontend = $Frontend.IsPresent
}

$anySwitch = $Infra -or $Backend -or $Customer -or $Provider -or $Admin -or $Frontend -or $All

if ($All) {
    foreach ($key in @($selected.Keys)) { $selected[$key] = $true }
}

if (-not $anySwitch) {
    $chosen = Select-Services `
        -Keys @($serviceLabels.Keys) `
        -Labels @($serviceLabels.Values) `
        -PreChecked @('Infra', 'Backend')

    if ($null -eq $chosen) {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    foreach ($key in $chosen) { $selected[$key] = $true }
}

if (-not ($selected.Values -contains $true)) {
    Write-Host "Nothing selected. Exiting." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# --- Infrastructure ---
if ($selected.Infra) {
    if (-not (Test-CommandExists "docker")) {
        Write-Host "docker not found on PATH - skipping infrastructure." -ForegroundColor Red
    } else {
        Write-Host "Starting infrastructure containers..." -ForegroundColor Cyan
        docker info > $null 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Docker Desktop does not appear to be running. Start it, then re-run with -Infra." -ForegroundColor Red
        } else {
            Push-Location $repoRoot
            docker-compose -f docker-compose.infrastructure.yml up -d
            Pop-Location
            Write-Host "Infra up. Postgres: localhost:54321  Redis: localhost:16379  Seq: http://localhost:5341  pgAdmin: http://localhost:5050" -ForegroundColor Green
        }
    }
    Write-Host ""
}

# --- Backend ---
if ($selected.Backend) {
    if (-not (Test-CommandExists "dotnet")) {
        Write-Host ".NET SDK not found on PATH - skipping backend." -ForegroundColor Red
    } else {
        if ($selected.Infra) {
            Write-Host "Waiting for Postgres to report healthy before starting the backend..." -ForegroundColor Yellow
            $healthy = $false
            for ($i = 0; $i -lt 15; $i++) {
                $status = docker inspect --format='{{.State.Health.Status}}' booksy-postgres 2>$null
                if ($status -eq 'healthy') { $healthy = $true; break }
                Start-Sleep -Seconds 2
            }
            if (-not $healthy) {
                Write-Host "Postgres not confirmed healthy yet - starting backend anyway; it will retry its own connection." -ForegroundColor Yellow
            }
        }
        $runCommand = "dotnet run --project src\Host\Booksy.Host\Booksy.Host.csproj"
        if ($NoOtpSandbox) {
            Write-Host "OTP sandbox pinning disabled - verification codes will be random (check the backend log)." -ForegroundColor Yellow
        } else {
            Write-Host "OTP codes pinned to '$OtpSandboxCode' for this run (pass -NoOtpSandbox to disable)." -ForegroundColor Cyan
            $runCommand = "`$env:OTP_SANDBOX_CODE = '$OtpSandboxCode'; $runCommand"
        }
        Start-InNewWindow -Title "Booksy Backend (:5000)" `
            -WorkingDirectory $repoRoot `
            -Command $runCommand `
            -BackgroundColor $serviceColors.Backend.Bg -ForegroundColor $serviceColors.Backend.Fg
    }
}

# --- Customer App (Flutter) ---
if ($selected.Customer) {
    if (-not (Test-CommandExists "flutter")) {
        Write-Host "flutter not found on PATH - skipping customer app." -ForegroundColor Red
    } else {
        Start-InNewWindow -Title "Booksy Customer App (Flutter)" `
            -WorkingDirectory (Join-Path $repoRoot "booksy-customer-app") `
            -Command "flutter run -d chrome" `
            -BackgroundColor $serviceColors.Customer.Bg -ForegroundColor $serviceColors.Customer.Fg
    }
}

# --- Provider App (Flutter) ---
if ($selected.Provider) {
    if (-not (Test-CommandExists "flutter")) {
        Write-Host "flutter not found on PATH - skipping provider app." -ForegroundColor Red
    } else {
        Start-InNewWindow -Title "Booksy Provider App (Flutter)" `
            -WorkingDirectory (Join-Path $repoRoot "booksy-provider-app") `
            -Command "flutter run -d chrome" `
            -BackgroundColor $serviceColors.Provider.Bg -ForegroundColor $serviceColors.Provider.Fg
    }
}

# --- Admin Panel (Vue) ---
if ($selected.Admin) {
    if (-not (Test-CommandExists "npm")) {
        Write-Host "npm not found on PATH - skipping admin panel." -ForegroundColor Red
    } else {
        Start-InNewWindow -Title "Booksy Admin Panel (Vite)" `
            -WorkingDirectory (Join-Path $repoRoot "booksy-admin") `
            -Command "npm run dev" `
            -BackgroundColor $serviceColors.Admin.Bg -ForegroundColor $serviceColors.Admin.Fg
    }
}

# --- Customer Web (legacy Vue frontend) ---
if ($selected.Frontend) {
    if (-not (Test-CommandExists "npm")) {
        Write-Host "npm not found on PATH - skipping customer web frontend." -ForegroundColor Red
    } else {
        Start-InNewWindow -Title "Booksy Customer Web (Vite)" `
            -WorkingDirectory (Join-Path $repoRoot "booksy-frontend") `
            -Command "npm run dev" `
            -BackgroundColor $serviceColors.Frontend.Bg -ForegroundColor $serviceColors.Frontend.Fg
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Done" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Each service runs in its own window - Ctrl+C or close the window to stop it individually," -ForegroundColor White
Write-Host "or run .\stop-all.ps1 to stop everything this launcher may have started." -ForegroundColor White
