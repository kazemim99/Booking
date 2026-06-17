#!/bin/bash

# Booksy - Run All Services (Development)
# This script starts all backend APIs and the frontend application

echo "========================================"
echo "  Booksy - Starting All Services"
echo "========================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found. Please install .NET SDK first.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ .NET SDK version: $(dotnet --version)${NC}"

if ! command -v node &> /dev/null; then
    echo -e "${RED}✗ Node.js not found. Please install Node.js first.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Node.js version: $(node --version)${NC}"

echo ""
echo -e "${YELLOW}Starting backend services...${NC}"

# Function to start service in background
start_service() {
    local title=$1
    local project_path=$2
    local port=$3

    echo -e "${GREEN}Starting $title on port $port...${NC}"
    cd "$SCRIPT_DIR/$project_path"
    dotnet run > "$SCRIPT_DIR/logs/${title// /_}.log" 2>&1 &
    echo $! >> "$SCRIPT_DIR/.pids"
    cd "$SCRIPT_DIR"
    sleep 2
}

# Create logs directory if it doesn't exist
mkdir -p "$SCRIPT_DIR/logs"

# Create/clear PID file
echo "" > "$SCRIPT_DIR/.pids"

# Start backend services
start_service "UserManagement API" "src/UserManagement/Booksy.UserManagement.API" "5001"
start_service "ServiceCatalog API" "src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api" "5002"
start_service "API Gateway" "src/APIGateway/Booksy.Gateway" "5000"

# Wait for backend services to initialize
echo ""
echo -e "${YELLOW}Waiting for backend services to initialize (15 seconds)...${NC}"
sleep 15

# Check if npm install is needed for frontend
FRONTEND_PATH="$SCRIPT_DIR/booksy-frontend"
if [ ! -d "$FRONTEND_PATH/node_modules" ]; then
    echo ""
    echo -e "${YELLOW}Installing frontend dependencies...${NC}"
    cd "$FRONTEND_PATH"
    npm install
    cd "$SCRIPT_DIR"
fi

# Start frontend
echo ""
echo -e "${YELLOW}Starting frontend application...${NC}"
cd "$FRONTEND_PATH"
npm run dev > "$SCRIPT_DIR/logs/Frontend.log" 2>&1 &
echo $! >> "$SCRIPT_DIR/.pids"
cd "$SCRIPT_DIR"

echo ""
echo -e "${GREEN}========================================"
echo "  All Services Started Successfully!"
echo "========================================${NC}"
echo ""
echo -e "${CYAN}Service URLs:${NC}"
echo "  - UserManagement API:  http://localhost:5001"
echo "  - ServiceCatalog API:  http://localhost:5002"
echo "  - API Gateway:         http://localhost:5000"
echo "  - Frontend:            http://localhost:5173"
echo ""
echo -e "${CYAN}Swagger Documentation:${NC}"
echo "  - UserManagement:      http://localhost:5001/swagger"
echo "  - ServiceCatalog:      http://localhost:5002/swagger"
echo "  - Gateway:             http://localhost:5000/swagger"
echo ""
echo -e "${CYAN}Logs are available in: $SCRIPT_DIR/logs/${NC}"
echo ""
echo -e "${YELLOW}To stop all services, run: ./stop-all.sh${NC}"
echo ""
