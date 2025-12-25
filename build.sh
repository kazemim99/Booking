#!/bin/bash

# Docker Build Optimization Script
# This script builds Docker images with layer caching optimizations

echo "Building Docker images with optimized layer caching..."
echo ""
echo "Optimizations enabled:"
echo "  - Parallel builds across services"
echo "  - Layer caching for NuGet packages"
echo "  - Layer caching for npm packages"
echo "  - Selective file copying to maximize cache hits"
echo ""

# Build with docker-compose in parallel
docker compose build --parallel

echo ""
echo "Build complete!"
echo ""
echo "To rebuild a specific service:"
echo "  docker compose build <service-name>"
echo ""
echo "Examples:"
echo "  docker compose build booksy-gateway"
echo "  docker compose build booksy-user-management-api"
echo "  docker compose build booksy-service-catalog-api"
echo "  docker compose build booksy-frontend"
echo ""
echo "To force rebuild without cache:"
echo "  docker compose build --no-cache <service-name>"
