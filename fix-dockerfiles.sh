#!/bin/bash

# Script to fix Dockerfiles by removing BuildKit cache mounts
# Run this on your Ubuntu server

echo "Fixing Dockerfiles to remove BuildKit cache mounts..."
echo ""

# Fix User Management API Dockerfile
echo "Updating User Management API Dockerfile..."
sed -i 's/RUN --mount=type=cache,id=nuget,target=\/root\/.nuget\/packages \\/RUN /g' \
  src/UserManagement/Booksy.UserManagement.API/Dockerfile

# Fix API Gateway Dockerfile
echo "Updating API Gateway Dockerfile..."
sed -i 's/RUN --mount=type=cache,id=nuget,target=\/root\/.nuget\/packages \\/RUN /g' \
  src/APIGateway/Booksy.Gateway/Dockerfile

# Fix Service Catalog API Dockerfile
echo "Updating Service Catalog API Dockerfile..."
sed -i 's/RUN --mount=type=cache,id=nuget,target=\/root\/.nuget\/packages \\/RUN /g' \
  src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Dockerfile

# Fix Frontend Dockerfile
echo "Updating Frontend Dockerfile..."
sed -i 's/RUN --mount=type=cache,id=npm,target=\/root\/.npm \\/RUN /g' \
  booksy-frontend/Dockerfile

# Remove continuation lines that are now orphaned
sed -i '/^[[:space:]]*dotnet restore/s/^[[:space:]]*//g' \
  src/UserManagement/Booksy.UserManagement.API/Dockerfile \
  src/APIGateway/Booksy.Gateway/Dockerfile \
  src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Dockerfile

sed -i '/^[[:space:]]*npm ci/s/^[[:space:]]*//g' \
  booksy-frontend/Dockerfile

echo ""
echo "✓ All Dockerfiles updated!"
echo ""
echo "Now you can run: docker compose build --parallel"
