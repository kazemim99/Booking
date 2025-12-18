@echo off
echo ========================================
echo Applying Gallery RowVersion Fix
echo ========================================
echo.

echo Step 1: Stop the API if it's running
echo Please stop the API manually, then press any key to continue...
pause

echo.
echo Step 2: Creating migration...
cd ..\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api

dotnet ef migrations add RemoveGalleryImageRowVersion ^
  --context ServiceCatalogDbContext ^
  --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj" ^
  --output-dir "Persistence/Migrations"

if %ERRORLEVEL% NEQ 0 (
    echo Migration creation failed!
    pause
    exit /b 1
)

echo.
echo Step 3: Applying migration to database...
dotnet ef database update ^
  --context ServiceCatalogDbContext ^
  --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj"

if %ERRORLEVEL% NEQ 0 (
    echo Migration application failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS!
echo ========================================
echo.
echo The fix has been applied:
echo - RowVersion removed from GalleryImage code configuration
echo - Migration created and applied
echo - Database schema updated (no actual changes needed)
echo.
echo You can now restart your API.
echo.
pause
