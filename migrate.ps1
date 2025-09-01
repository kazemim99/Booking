# Migration Commands for Windows PowerShell (save as migrate.ps1)

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("add", "update", "remove", "list", "script")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$MigrationName
)

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"

Write-Host "Booksy Database Migration Tool" -ForegroundColor $Green
Write-Host "================================" -ForegroundColor $Green

function Run-Migrations {
    param($Action, $MigrationName)
    
    Write-Host "Running database migrations..." -ForegroundColor $Yellow
    
    Push-Location "src/UserManagement/Booksy.UserManagement.Infrastructure"
    
    try {
        switch ($Action) {
            "add" {
                if (-not $MigrationName) {
                    Write-Host "Migration name is required for 'add' action" -ForegroundColor $Red
                    return
                }
                Write-Host "Adding new migration: $MigrationName" -ForegroundColor $Yellow
                dotnet ef migrations add $MigrationName `
                    --startup-project "../Booksy.UserManagement.API" `
                    --context "UserManagementDbContext" `
                    --output-dir "Persistence/Migrations"
            }
            
            "update" {
                Write-Host "Updating database..." -ForegroundColor $Yellow
                dotnet ef database update `
                    --startup-project "../Booksy.UserManagement.API" `
                    --context "UserManagementDbContext"
            }
            
            "remove" {
                Write-Host "Removing last migration..." -ForegroundColor $Yellow
                dotnet ef migrations remove `
                    --startup-project "../Booksy.UserManagement.API" `
                    --context "UserManagementDbContext"
            }
            
            "list" {
                Write-Host "Listing migrations..." -ForegroundColor $Yellow
                dotnet ef migrations list `
                    --startup-project "../Booksy.UserManagement.API" `
                    --context "UserManagementDbContext"
            }
            
            "script" {
                Write-Host "Generating SQL script..." -ForegroundColor $Yellow
                # Ensure scripts directory exists
                if (-not (Test-Path "../../../scripts")) {
                    New-Item -ItemType Directory -Path "../../../scripts" -Force
                }
                dotnet ef migrations script `
                    --startup-project "../Booksy.UserManagement.API" `
                    --context "UserManagementDbContext" `
                    --output "../../../scripts/migrations.sql"
            }
        }
    }
    catch {
        Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor $Red
    }
    finally {
        Pop-Location
    }
}

# Check if EF Core tools are installed
try {
    dotnet ef --version | Out-Null
} catch {
    Write-Host "Installing EF Core tools..." -ForegroundColor $Yellow
    dotnet tool install --global dotnet-ef
}

# Run the migration command
Run-Migrations -Action $Action -MigrationName $MigrationName

Write-Host "Migration process completed!" -ForegroundColor $Green