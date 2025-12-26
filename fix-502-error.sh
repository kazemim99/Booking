#!/bin/bash

# Diagnostic and Fix Script for 502 Bad Gateway Errors
# Run this script on your production server

echo "================================================"
echo "Booksy 502 Error Diagnostic & Fix Script"
echo "================================================"
echo ""

# Change to deployment directory
DEPLOY_PATH="${1:-/opt/booksy}"
cd "$DEPLOY_PATH" || { echo "Error: Cannot access $DEPLOY_PATH"; exit 1; }

echo "1. Checking container status..."
echo "------------------------------------------------"
docker ps --format 'table {{.Names}}\t{{.Status}}'
echo ""

echo "2. Checking for unhealthy containers..."
echo "------------------------------------------------"
UNHEALTHY=$(docker ps --filter "health=unhealthy" --format "{{.Names}}")
if [ -n "$UNHEALTHY" ]; then
    echo "⚠️  Unhealthy containers found:"
    echo "$UNHEALTHY"
else
    echo "✅ No unhealthy containers"
fi
echo ""

echo "3. Checking if containers are running..."
echo "------------------------------------------------"
SERVICES=("booksy-usermanagement-api" "booksy-servicecatalog-api" "booksy-gateway" "booksy-frontend")
for service in "${SERVICES[@]}"; do
    if docker ps --format '{{.Names}}' | grep -q "^${service}$"; then
        STATUS=$(docker inspect --format='{{.State.Status}}' "$service" 2>/dev/null)
        HEALTH=$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}no healthcheck{{end}}' "$service" 2>/dev/null)
        echo "  $service: Status=$STATUS, Health=$HEALTH"
    else
        echo "  ❌ $service: NOT RUNNING"
    fi
done
echo ""

echo "4. Checking recent logs for errors..."
echo "------------------------------------------------"
for service in "${SERVICES[@]}"; do
    if docker ps --format '{{.Names}}' | grep -q "^${service}$"; then
        echo "📋 Last 10 lines from $service:"
        docker logs --tail 10 "$service" 2>&1 | head -20
        echo ""
    fi
done

echo "5. Testing health endpoints..."
echo "------------------------------------------------"
echo "Testing if curl is available in containers..."
for service in "booksy-usermanagement-api" "booksy-servicecatalog-api" "booksy-gateway"; do
    if docker ps --format '{{.Names}}' | grep -q "^${service}$"; then
        CURL_CHECK=$(docker exec "$service" which curl 2>/dev/null)
        if [ -n "$CURL_CHECK" ]; then
            echo "  ✅ $service: curl found at $CURL_CHECK"
            # Try to hit health endpoint
            HEALTH_RESPONSE=$(docker exec "$service" curl -s -o /dev/null -w "%{http_code}" http://localhost:80/health 2>/dev/null || echo "FAILED")
            echo "     Health endpoint response: $HEALTH_RESPONSE"
        else
            echo "  ❌ $service: curl NOT FOUND (health checks will fail!)"
        fi
    fi
done
echo ""

echo "6. Proposed Fix..."
echo "------------------------------------------------"
echo "Based on the diagnostic results above, here are the recommended actions:"
echo ""

# Check if any service is unhealthy or missing curl
NEEDS_FIX=false
for service in "booksy-usermanagement-api" "booksy-servicecatalog-api" "booksy-gateway"; do
    if docker ps --format '{{.Names}}' | grep -q "^${service}$"; then
        CURL_CHECK=$(docker exec "$service" which curl 2>/dev/null)
        if [ -z "$CURL_CHECK" ]; then
            NEEDS_FIX=true
            echo "  ⚠️  $service is missing curl"
        fi
    fi
done

if [ "$NEEDS_FIX" = true ]; then
    echo ""
    echo "❌ CRITICAL: Services are missing curl, health checks will fail!"
    echo ""
    echo "The Docker images need to be rebuilt with curl installed."
    echo "Please check the Dockerfiles and ensure they include:"
    echo ""
    echo "For .NET services:"
    echo "  RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*"
    echo ""
    echo "For nginx (frontend):"
    echo "  RUN apk add --no-cache curl"
    echo ""
    echo "After fixing the Dockerfiles, rebuild and push the images, then redeploy."
else
    echo "✅ All services have curl installed"
    echo ""
    echo "Attempting to restart unhealthy services..."
    docker-compose -f docker-compose.prod.yml restart
    echo ""
    echo "Waiting 30 seconds for services to become healthy..."
    sleep 30
    echo ""
    echo "Updated container status:"
    docker ps --format 'table {{.Names}}\t{{.Status}}'
fi

echo ""
echo "================================================"
echo "Diagnostic Complete"
echo "================================================"
