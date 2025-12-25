# Docker Build Optimization Script for Windows
# This script builds Docker images with layer caching optimizations

Write-Host "Building Docker images with optimized layer caching..." -ForegroundColor Green
Write-Host ""
Write-Host "Optimizations enabled:" -ForegroundColor Cyan
Write-Host "  - Parallel builds across services"
Write-Host "  - Layer caching for NuGet packages"
Write-Host "  - Layer caching for npm packages"
Write-Host "  - Selective file copying to maximize cache hits"
Write-Host ""

# Build with docker-compose in parallel
docker compose build --parallel

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To rebuild a specific service:" -ForegroundColor Yellow
Write-Host "  docker compose build <service-name>"
Write-Host ""
Write-Host "Examples:" -ForegroundColor Cyan
Write-Host "  docker compose build booksy-gateway"
Write-Host "  docker compose build booksy-user-management-api"
Write-Host "  docker compose build booksy-service-catalog-api"
Write-Host "  docker compose build booksy-frontend"
Write-Host ""
Write-Host "To force rebuild without cache:" -ForegroundColor Yellow
Write-Host "  docker compose build --no-cache <service-name>"
