# Booksy Infrastructure Setup for Visual Studio Debugging
# This script starts only infrastructure services (PostgreSQL, Redis, RabbitMQ, Seq)
# Backend APIs should be run in Visual Studio for debugging

Write-Host "🚀 Starting Booksy Infrastructure Services..." -ForegroundColor Green
Write-Host ""

# Start infrastructure services
docker-compose -f docker-compose.infrastructure.yml up -d

Write-Host ""
Write-Host "✅ Infrastructure services started!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Service Status:" -ForegroundColor Cyan
docker-compose -f docker-compose.infrastructure.yml ps

Write-Host ""
Write-Host "🔗 Service URLs:" -ForegroundColor Cyan
Write-Host "  PostgreSQL:   localhost:54321 (user: booksy_admin, pass: Booksy@2024!)" -ForegroundColor White
Write-Host "  Redis:        localhost:6379 (pass: Redis@2024!)" -ForegroundColor White
Write-Host "  RabbitMQ:     localhost:15672 (user: booksy_admin, pass: Booksy@2024!)" -ForegroundColor White
Write-Host "  Seq Logs:     http://localhost:5341 (user: admin, pass: Booksy@2024!)" -ForegroundColor White
Write-Host "  pgAdmin:      http://localhost:5050 (email: admin@booksy.local, pass: Booksy@2024!)" -ForegroundColor White

Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Open Booksy.sln in Visual Studio" -ForegroundColor White
Write-Host "  2. Set Multiple Startup Projects:" -ForegroundColor White
Write-Host "     - Booksy.UserManagement.API (Start)" -ForegroundColor White
Write-Host "     - Booksy.ServiceCatalog.Api (Start)" -ForegroundColor White
Write-Host "     - Booksy.Gateway (Start)" -ForegroundColor White
Write-Host "  3. Select 'http' profile (not https or Docker)" -ForegroundColor White
Write-Host "  4. Press F5 to debug" -ForegroundColor White

Write-Host ""
Write-Host "💡 Tips:" -ForegroundColor Cyan
Write-Host "  - Set breakpoints in your backend code" -ForegroundColor White
Write-Host "  - View logs in Seq: http://localhost:5341" -ForegroundColor White
Write-Host "  - Check service health: docker-compose -f docker-compose.infrastructure.yml ps" -ForegroundColor White
Write-Host "  - Stop infrastructure: docker-compose -f docker-compose.infrastructure.yml down" -ForegroundColor White
Write-Host ""
