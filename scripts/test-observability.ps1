# ========================================
# Test Observability Setup Script
# ========================================
# This script tests the logging and observability infrastructure

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Booksy Observability Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if Docker is running
Write-Host "[1/5] Checking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Start Seq container
Write-Host "[2/5] Starting Seq container..." -ForegroundColor Yellow
docker-compose up -d seq
Start-Sleep -Seconds 5

# Check if Seq is running
$seqRunning = docker ps --filter "name=booksy-seq" --format "{{.Status}}"
if ($seqRunning) {
    Write-Host "✅ Seq container is running: $seqRunning" -ForegroundColor Green
} else {
    Write-Host "❌ Seq container failed to start" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Check Seq health
Write-Host "[3/5] Checking Seq health..." -ForegroundColor Yellow
Start-Sleep -Seconds 10  # Wait for Seq to fully start

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5341" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Seq UI is accessible at http://localhost:5341" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Seq UI not yet ready (this is normal, may take 30-60 seconds)" -ForegroundColor Yellow
    Write-Host "   You can check manually at http://localhost:5341" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Show Seq logs
Write-Host "[4/5] Seq container logs (last 20 lines):" -ForegroundColor Yellow
docker logs --tail 20 booksy-seq
Write-Host ""

# Step 5: Summary and next steps
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Access Points:" -ForegroundColor Yellow
Write-Host "   • Seq UI: http://localhost:5341" -ForegroundColor White
Write-Host "   • Username: admin" -ForegroundColor White
Write-Host "   • Password: Booksy@2024!" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Start ServiceCatalog API:" -ForegroundColor White
Write-Host "      dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. Start UserManagement API:" -ForegroundColor White
Write-Host "      dotnet run --project src/UserManagement/Booksy.UserManagement.API" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. Make some API requests to generate logs" -ForegroundColor White
Write-Host ""
Write-Host "   4. View logs in Seq at http://localhost:5341" -ForegroundColor White
Write-Host ""
Write-Host "📖 Documentation:" -ForegroundColor Yellow
Write-Host "   See OBSERVABILITY-SETUP.md for detailed usage guide" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
