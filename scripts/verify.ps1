<#
.SYNOPSIS
  The checkable definition of done. Runs the verification tier and records the result.

.DESCRIPTION
  FAST  build Booksy.sln + every unit/architecture test project. No Docker.
  FULL  FAST + Host composition + both integration suites (Testcontainers Postgres, needs
        Docker) + type-check/lint in each touched Vue app + analyze/test in each touched
        Flutter app.

  Writes .verify/status.json { sha, tree, tier, result, steps[] } so the Stop hook can tell
  whether the current working tree has a green FULL run behind it. `tree` is a git tree hash
  of the working tree (tracked + untracked, .gitignore respected), so any edit after the run
  makes the result stale.

.PARAMETER Tier      fast | full   (default fast)
.PARAMETER Filter    Extra `dotnet test --filter` applied to the integration projects only,
                     e.g. -Filter "FullyQualifiedName~AvailabilityStaffIsolation".
.PARAMETER All       Run the frontend/Flutter steps even when git shows them untouched.
.PARAMETER SkipBuild Skip the solution build (use when you just built).
.PARAMETER IncludeFeatures Also run the Reqnroll Gherkin features in ServiceCatalog.IntegrationTests.
                     Excluded by default: REQNROLL-COVERAGE-GAP.md documents them as a spec backlog
                     (unbound steps) and FOLLOW-UPS #31 as credentials-blocked, so they fail by design.

.EXAMPLE
  scripts/verify.ps1                       # FAST
  scripts/verify.ps1 -Tier full            # FULL
  scripts/verify.ps1 -Tier full -Filter "FullyQualifiedName~Membership"
#>
[CmdletBinding()]
param(
    [ValidateSet('fast', 'full')] [string]$Tier = 'fast',
    [string]$Filter = '',
    [switch]$All,
    [switch]$SkipBuild,
    [switch]$IncludeFeatures
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$verifyDir = Join-Path $root '.verify'
$logDir = Join-Path $verifyDir 'logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$startedAt = Get-Date
$script:steps = @()

# ---------------------------------------------------------------- helpers
function Write-Head($text) { Write-Host ""; Write-Host "== $text" -ForegroundColor Cyan }

function Invoke-Step {
    param([string]$Name, [string]$Dir, [string]$Command, [string[]]$ShowPattern, [switch]$NoTail)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $log = Join-Path $logDir (($Name -replace '[^A-Za-z0-9._-]', '_') + '.log')
    Write-Head "$Name"
    Write-Host "   $Command" -ForegroundColor DarkGray
    Push-Location $Dir
    try {
        # cmd /c keeps native stderr out of PowerShell's error stream (5.1 wraps it otherwise)
        cmd /c "$Command > `"$log`" 2>&1"
        $code = $LASTEXITCODE
    }
    finally { Pop-Location }
    $sw.Stop()
    $lines = @(Get-Content $log -ErrorAction SilentlyContinue)
    $result = if ($code -eq 0) { 'pass' } else { 'fail' }
    if ($ShowPattern) {
        $lines | Where-Object { $l = $_; ($ShowPattern | Where-Object { $l -match $_ }).Count -gt 0 } |
            Select-Object -Unique | ForEach-Object { Write-Host "   $_" }
    }
    if ($code -ne 0 -and -not $NoTail) {
        Write-Host "   --- last 40 lines of $log ---" -ForegroundColor Yellow
        $lines | Select-Object -Last 40 | ForEach-Object { Write-Host "   $_" }
    }
    $color = if ($result -eq 'pass') { 'Green' } else { 'Red' }
    Write-Host ("   {0}  {1:n0}s" -f $result.ToUpper(), $sw.Elapsed.TotalSeconds) -ForegroundColor $color
    $script:steps += [pscustomobject]@{ name = $Name; result = $result; seconds = [math]::Round($sw.Elapsed.TotalSeconds, 1); log = $log }
}

function Add-Blocked([string]$Name, [string]$Why) {
    Write-Head $Name
    Write-Host "   BLOCKED: $Why" -ForegroundColor Yellow
    $script:steps += [pscustomobject]@{ name = $Name; result = 'blocked'; seconds = 0; log = $null; reason = $Why }
}

function Get-TouchedPaths {
    $paths = @()
    $paths += git diff --name-only HEAD 2>$null
    $paths += git ls-files --others --exclude-standard 2>$null
    $base = git merge-base HEAD master 2>$null
    if ($base) { $paths += git diff --name-only "$base" HEAD 2>$null }
    return @($paths | Where-Object { $_ } | Select-Object -Unique)
}

function Get-TreeHash {
    # Hash of the working tree (tracked + untracked, .gitignore respected) without touching
    # the real index. The stop hook computes the same value to detect edits after this run.
    $tmpIndex = Join-Path $verifyDir 'index'
    $old = $env:GIT_INDEX_FILE
    try {
        $env:GIT_INDEX_FILE = $tmpIndex
        cmd /c "git read-tree HEAD 2>nul" | Out-Null
        cmd /c "git -c core.safecrlf=false add -A 2>nul" | Out-Null
        $tree = (cmd /c "git write-tree 2>nul").Trim()
    }
    finally {
        if ($null -eq $old) { Remove-Item Env:GIT_INDEX_FILE -ErrorAction SilentlyContinue } else { $env:GIT_INDEX_FILE = $old }
        Remove-Item $tmpIndex -ErrorAction SilentlyContinue
    }
    return $tree
}

# tests/known-failures.txt: the recorded baseline of integration tests that already failed before
# a change (see the file header). A db step whose failures are ALL on the list passes with a
# note; any failure off the list keeps it red. Listed tests that did not fail are reported so
# the list can shrink. The list is never consulted for FAST (unit) steps.
$knownFailures = @()
$knownFile = Join-Path $root 'tests/known-failures.txt'
if (Test-Path $knownFile) {
    $knownFailures = @(Get-Content $knownFile | Where-Object { $_ -and -not $_.StartsWith('#') } | ForEach-Object { $_.Trim() })
}
function Resolve-KnownFailures([string]$StepName) {
    $step = $script:steps[-1]
    if ($step.name -ne $StepName -or $step.result -ne 'fail' -or -not $step.log) { return }
    $failing = @(Get-Content $step.log -ErrorAction SilentlyContinue |
        Where-Object { $_ -match '\[FAIL\]\s*$' } |
        ForEach-Object { ($_ -replace '^\[xUnit\.net [0-9:.]+\]\s+', '') -replace '\s*\[FAIL\]\s*$', '' } |
        Select-Object -Unique)
    if (-not $failing.Count) { return }   # failed for another reason (build, crash): stays red
    $unexpected = @($failing | Where-Object { $knownFailures -notcontains $_ })
    $known = @($failing | Where-Object { $knownFailures -contains $_ })
    if ($unexpected.Count) {
        Write-Host ("   {0} failure(s) NOT on tests/known-failures.txt (regressions until proven otherwise):" -f $unexpected.Count) -ForegroundColor Red
        $unexpected | ForEach-Object { Write-Host "     $_" -ForegroundColor Red }
        if ($known.Count) { Write-Host ("   plus {0} known-baseline failure(s)" -f $known.Count) -ForegroundColor Yellow }
        return
    }
    $step.result = 'pass'
    $step | Add-Member -NotePropertyName knownFailures -NotePropertyValue $known.Count -Force
    Write-Host ("   PASS with {0} known-baseline failure(s) (all listed in tests/known-failures.txt)" -f $known.Count) -ForegroundColor Yellow
}

$testShow = @('^\s*Failed ', 'Passed!', 'Failed!', 'error [A-Z]+[0-9]+', 'Total tests', 'No test is available')
$buildShow = @('error [A-Z]+[0-9]+', 'Build succeeded', 'Build FAILED')

# ---------------------------------------------------------------- FAST
Write-Host "verify  tier=$Tier  root=$root" -ForegroundColor Cyan

if (-not $SkipBuild) {
    # Another test host holding the built DLLs fails the copy step with MSB3027/MSB3021 after
    # 10 retries per file. Say so up front. It is NOT safe to kill it: on this machine several
    # assistant sessions share the checkout, and a long-running testhost is far more likely to
    # be a peer's integration run than a leftover. Wait, or coordinate with the peer session.
    $running = @(Get-Process testhost -ErrorAction SilentlyContinue)
    if ($running.Count) {
        Write-Host ("   NOTE: {0} testhost process(es) running (PIDs {1}, oldest started {2:HH:mm}). If this build fails with MSB3027 'file is locked by testhost', another session is testing: wait for it or ask it — do not kill it." -f $running.Count, ($running.Id -join ', '), ($running | Sort-Object StartTime | Select-Object -First 1).StartTime) -ForegroundColor Yellow
    }
    Invoke-Step -Name 'build' -Dir $root -Command 'dotnet build Booksy.sln --nologo -v q' -ShowPattern $buildShow -NoTail
}

$unitProjects = @(
    'tests/Booksy.Core.Domain.UnitTests',
    'tests/Booksy.Infrastructure.Core.UnitTests',
    'tests/Booksy.ServiceCatalog.Domain.UnitTests',
    'tests/Booksy.ServiceCatalog.Application.UnitTests',
    'tests/Booksy.ServiceCatalog.UnitTests',
    'tests/Booksy.UserManagement.Application.UnitTests',
    'tests/Booksy.ArchitectureTests'
)
foreach ($p in $unitProjects) {
    if (-not (Test-Path (Join-Path $root "$p/*.csproj"))) { continue }   # skip dirs without a project
    $name = Split-Path $p -Leaf
    Invoke-Step -Name "unit:$name" -Dir $root -Command "dotnet test $p --nologo -v q" -ShowPattern $testShow
}

# ---------------------------------------------------------------- FULL
if ($Tier -eq 'full') {
    $dockerOk = $false
    cmd /c "docker info >nul 2>&1"; if ($LASTEXITCODE -eq 0) { $dockerOk = $true }

    $dbProjects = @(
        'tests/Booksy.Host.CompositionTests',
        'tests/Booksy.ServiceCatalog.IntegrationTests',
        'tests/Booksy.UserManagement.IntegrationTests'
    )
    foreach ($p in $dbProjects) {
        $name = Split-Path $p -Leaf
        if (-not $dockerOk) { Add-Blocked "db:$name" 'Docker is not running; Testcontainers cannot start Postgres'; continue }
        $cmd = "dotnet test $p --nologo -v q"
        $clauses = @()
        if ($p -like '*ServiceCatalog.IntegrationTests' -and -not $IncludeFeatures) {
            # The Reqnroll Gherkin features under Features/ are a specification backlog, not a
            # test suite: openspec/changes/REQNROLL-COVERAGE-GAP.md records 707 of 739 scenarios
            # blocked by unbound steps, and FOLLOW-UPS #31 records the payment scenarios as
            # credentials-blocked. They fail by design, so they are not a definition-of-done gate.
            # Run them deliberately with -IncludeFeatures (or -Filter "FullyQualifiedName~Features").
            $clauses += 'FullyQualifiedName!~IntegrationTests.Features'
        }
        if ($Filter -and $p -like '*IntegrationTests') { $clauses += "($Filter)" }
        if ($clauses.Count) { $cmd += " --filter `"$($clauses -join '&')`"" }
        Invoke-Step -Name "db:$name" -Dir $root -Command $cmd -ShowPattern $testShow
        Resolve-KnownFailures -StepName "db:$name"
    }

    $touched = Get-TouchedPaths
    function Touched($prefix) { $All -or (($touched | Where-Object { $_ -like "$prefix/*" }).Count -gt 0) }

    foreach ($app in @('booksy-frontend', 'booksy-admin')) {
        if (-not (Touched $app)) { continue }
        if (-not (Test-Path (Join-Path $root "$app/node_modules"))) { Add-Blocked "vue:$app" "no node_modules; run 'npm ci' in $app"; continue }
        Invoke-Step -Name "vue:${app}:type-check" -Dir (Join-Path $root $app) -Command 'npm run --silent type-check' -ShowPattern @('error TS', 'Found [0-9]+ error')
        if ($app -eq 'booksy-frontend') {
            Invoke-Step -Name "vue:${app}:lint" -Dir (Join-Path $root $app) -Command 'npm run --silent lint:check' -ShowPattern @('error', 'problems')
        }
    }

    foreach ($app in @('booksy-customer-app', 'booksy-provider-app')) {
        if (-not (Touched $app)) { continue }
        if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) { Add-Blocked "flutter:$app" 'flutter not on PATH'; continue }
        Invoke-Step -Name "flutter:${app}:analyze" -Dir (Join-Path $root $app) -Command 'flutter analyze --no-pub --no-fatal-warnings --no-fatal-infos' -ShowPattern @('error •', 'No issues found', 'issues found')
        Invoke-Step -Name "flutter:${app}:test" -Dir (Join-Path $root $app) -Command 'flutter test --no-pub' -ShowPattern @('^\+[0-9]+ -[0-9]+', 'All tests passed', 'Some tests failed', '\[E\]')
    }
}

# ---------------------------------------------------------------- status
$failed = @($script:steps | Where-Object { $_.result -eq 'fail' })
$blocked = @($script:steps | Where-Object { $_.result -eq 'blocked' })
$result = if ($failed.Count -gt 0) { 'fail' } elseif ($blocked.Count -gt 0) { 'blocked' } else { 'pass' }

$status = [ordered]@{
    sha        = (git rev-parse HEAD).Trim()
    tree       = Get-TreeHash
    tier       = $Tier
    result     = $result
    startedAt  = $startedAt.ToString('o')
    finishedAt = (Get-Date).ToString('o')
    seconds    = [math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)
    failed     = @($failed | ForEach-Object { $_.name })
    blocked    = @($blocked | ForEach-Object { $_.name })
    steps      = $script:steps
}
# WriteAllText with a BOM-less encoding: PowerShell 5.1's -Encoding utf8 prepends a BOM, which
# the bash hook tolerates but strict JSON readers reject.
[System.IO.File]::WriteAllText((Join-Path $verifyDir 'status.json'), ($status | ConvertTo-Json -Depth 4), (New-Object System.Text.UTF8Encoding $false))

Write-Host ""
$summaryColor = switch ($result) { 'pass' { 'Green' } 'blocked' { 'Yellow' } default { 'Red' } }
Write-Host ("verify {0}: {1}  ({2} steps, {3:n0}s)" -f $Tier.ToUpper(), $result.ToUpper(), $script:steps.Count, $status.seconds) -ForegroundColor $summaryColor
if ($failed.Count) { Write-Host ("  failed:  " + ($failed.name -join ', ')) -ForegroundColor Red }
if ($blocked.Count) { Write-Host ("  blocked: " + ($blocked.name -join ', ')) -ForegroundColor Yellow }
Write-Host "  status:  .verify/status.json"

exit $(if ($result -eq 'pass') { 0 } else { 1 })
