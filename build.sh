#!/bin/bash

# Docker Build Optimization Script
# This script enables BuildKit for significantly faster builds

echo "Building Docker images with BuildKit optimizations..."
echo ""
echo "This will:"
echo "  - Use Docker BuildKit for parallel builds"
echo "  - Cache NuGet packages across builds"
echo "  - Cache npm packages across builds"
echo "  - Reuse unchanged layers"
echo ""

# Enable BuildKit
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Build with docker-compose
docker compose build --parallel

echo ""
echo "Build complete!"
echo ""
echo "To rebuild a specific service:"
echo "  DOCKER_BUILDKIT=1 docker compose build <service-name>"
echo ""
echo "To force rebuild without cache:"
echo "  DOCKER_BUILDKIT=1 docker compose build --no-cache <service-name>"
