# Docker Build Optimization Script for Windows
# This script enables BuildKit for significantly faster builds

Write-Host "Building Docker images with BuildKit optimizations..." -ForegroundColor Green
Write-Host ""
Write-Host "This will:" -ForegroundColor Cyan
Write-Host "  - Use Docker BuildKit for parallel builds"
Write-Host "  - Cache NuGet packages across builds"
Write-Host "  - Cache npm packages across builds"
Write-Host "  - Reuse unchanged layers"
Write-Host ""

# Enable BuildKit
$env:DOCKER_BUILDKIT = 1
$env:COMPOSE_DOCKER_CLI_BUILD = 1

# Build with docker-compose
docker compose build --parallel

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To rebuild a specific service:" -ForegroundColor Yellow
Write-Host "  `$env:DOCKER_BUILDKIT=1; docker compose build <service-name>"
Write-Host ""
Write-Host "To force rebuild without cache:" -ForegroundColor Yellow
Write-Host "  `$env:DOCKER_BUILDKIT=1; docker compose build --no-cache <service-name>"
