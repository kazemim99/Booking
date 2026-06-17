# Stop Booksy Infrastructure Services

Write-Host "🛑 Stopping Booksy Infrastructure Services..." -ForegroundColor Yellow

docker-compose -f docker-compose.infrastructure.yml down

Write-Host ""
Write-Host "✅ Infrastructure services stopped!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 To remove volumes as well (WARNING: deletes all data):" -ForegroundColor Cyan
Write-Host "   docker-compose -f docker-compose.infrastructure.yml down -v" -ForegroundColor White
Write-Host ""
