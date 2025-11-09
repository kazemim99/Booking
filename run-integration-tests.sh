#!/bin/bash

# Integration Test Runner Script
# Checks prerequisites and runs integration tests

set -e

echo "🧪 Booksy Integration Test Runner"
echo "=================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check functions
check_backend() {
  local name=$1
  local url=$2

  if curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null | grep -q "200\|404"; then
    echo -e "${GREEN}✓${NC} $name is running at $url"
    return 0
  else
    echo -e "${RED}✗${NC} $name is NOT running at $url"
    return 1
  fi
}

check_env_var() {
  local var_name=$1

  if [ -f ".env.test" ] && grep -q "^${var_name}=" .env.test; then
    echo -e "${GREEN}✓${NC} $var_name is set"
    return 0
  else
    echo -e "${RED}✗${NC} $var_name is NOT set"
    return 1
  fi
}

# 1. Check Backend Services
echo "📡 Checking Backend Services..."
echo "--------------------------------"

SERVICE_CATALOG_OK=false
USER_MANAGEMENT_OK=false

if check_backend "ServiceCatalog API" "http://localhost:5010"; then
  SERVICE_CATALOG_OK=true
fi

if check_backend "UserManagement API" "http://localhost:5020"; then
  USER_MANAGEMENT_OK=true
fi

echo ""

# 2. Check Environment Variables
echo "🔐 Checking Environment Variables..."
echo "-------------------------------------"

cd "$(dirname "$0")/booksy-frontend" || exit 1

ENV_OK=true

if [ ! -f ".env.test" ]; then
  echo -e "${RED}✗${NC} .env.test file not found"
  ENV_OK=false
else
  check_env_var "TEST_PROVIDER_ID" || ENV_OK=false
  check_env_var "TEST_CUSTOMER_ID" || ENV_OK=false
  check_env_var "TEST_AUTH_TOKEN" || ENV_OK=false
fi

echo ""

# 3. Check Dependencies
echo "📦 Checking Dependencies..."
echo "---------------------------"

if [ ! -d "node_modules" ]; then
  echo -e "${YELLOW}⚠${NC}  node_modules not found. Running npm install..."
  npm install
else
  echo -e "${GREEN}✓${NC} Dependencies installed"
fi

echo ""

# 4. Decision Time
echo "🎯 Test Readiness Check"
echo "------------------------"

if [ "$SERVICE_CATALOG_OK" = true ] && [ "$USER_MANAGEMENT_OK" = true ] && [ "$ENV_OK" = true ]; then
  echo -e "${GREEN}✓${NC} All prerequisites met!"
  echo ""
  echo "🚀 Running integration tests..."
  echo ""

  npm run test:integration

elif [ "$SERVICE_CATALOG_OK" = false ] || [ "$USER_MANAGEMENT_OK" = false ]; then
  echo -e "${YELLOW}⚠${NC}  Backend services are not running"
  echo ""
  echo "To start backend services, open TWO terminals and run:"
  echo ""
  echo "  ${GREEN}Terminal 1:${NC}"
  echo "    cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api"
  echo "    dotnet run --launch-profile http"
  echo ""
  echo "  ${GREEN}Terminal 2:${NC}"
  echo "    cd src/UserManagement/Booksy.UserManagement.API"
  echo "    dotnet run --launch-profile http"
  echo ""
  echo "After starting, run this script again."
  exit 1

elif [ "$ENV_OK" = false ]; then
  echo -e "${YELLOW}⚠${NC}  Environment variables not configured"
  echo ""
  echo "Create a .env.test file in booksy-frontend/ directory:"
  echo ""
  echo "  ${GREEN}cd booksy-frontend${NC}"
  echo "  ${GREEN}cat > .env.test << 'EOF'${NC}"
  echo "  TEST_PROVIDER_ID=your-provider-uuid"
  echo "  TEST_CUSTOMER_ID=your-customer-uuid"
  echo "  TEST_AUTH_TOKEN=your-jwt-token"
  echo "  EOF"
  echo ""
  echo "To get a JWT token:"
  echo "  curl -X POST http://localhost:5020/api/v1/auth/login \\"
  echo "    -H \"Content-Type: application/json\" \\"
  echo "    -d '{\"email\": \"test@example.com\", \"password\": \"Test123!\"}' \\"
  echo "    | jq -r '.data.token'"
  echo ""
  exit 1
fi
