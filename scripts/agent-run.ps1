<#
.SYNOPSIS
  Unattended implementation runner: repeats `claude -p "/implement <change>"` until the change's
  tasks.md reports DONE or STOPPED(...), or the iteration cap is hit.

.DESCRIPTION
  Each iteration is a fresh non-interactive Claude Code session. The loop state lives in
  tasks.md, so a session that ends mid-list is resumed by the next one. Protected operations
  (push, PR, ef database update, prod compose, ssh) are on the `ask` list in
  .claude/settings.json; in -p mode they cannot be approved, so they are refused — which is
  the intended safety property of an unattended run.

  Every iteration's report is appended to openspec/changes/<change>/RUNS.md.

.EXAMPLE
  scripts/agent-run.ps1 refactor-identity-and-membership
  scripts/agent-run.ps1 _inline/fix-slot-release -MaxIterations 5
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Change,
    [int]$MaxIterations = 10,
    [string]$Model = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$tasks = Join-Path $root "openspec/changes/$Change/tasks.md"
if (-not (Test-Path $tasks)) { throw "no tasks.md at $tasks" }
$runs = Join-Path (Split-Path $tasks -Parent) 'RUNS.md'

function Get-Status { (Get-Content $tasks -TotalCount 1) -replace '^Status:\s*', '' }

for ($i = 1; $i -le $MaxIterations; $i++) {
    $status = Get-Status
    if ($status -match '^(DONE|STOPPED)') { Write-Host "tasks.md is $status; nothing to run."; break }
    Write-Host ("== iteration {0}/{1}  status={2}  {3}" -f $i, $MaxIterations, $status, (Get-Date -Format s)) -ForegroundColor Cyan
    $args = @('-p', "/implement $Change", '--permission-mode', 'acceptEdits', '--output-format', 'text')
    if ($Model) { $args += @('--model', $Model) }
    $out = & claude @args 2>&1 | Out-String
    Add-Content -Encoding utf8 $runs ("`n## Run {0} — {1}`n`n{2}`n" -f $i, (Get-Date -Format s), $out.Trim())
    Write-Host ($out | Select-Object -Last 1)
    $status = Get-Status
    if ($status -match '^(DONE|STOPPED)') { Write-Host "finished: $status" -ForegroundColor Green; break }
}
Write-Host ("final status: {0}  (reports in {1})" -f (Get-Status), $runs)
