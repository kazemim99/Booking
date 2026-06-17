#!/bin/bash

# Booksy - Stop All Services
# This script stops all running services

echo "========================================"
echo "  Booksy - Stopping All Services"
echo "========================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Check if PID file exists
if [ ! -f "$SCRIPT_DIR/.pids" ]; then
    echo -e "${YELLOW}No running services found.${NC}"
    exit 0
fi

# Read PIDs and kill processes
while IFS= read -r pid; do
    if [ ! -z "$pid" ]; then
        if ps -p "$pid" > /dev/null 2>&1; then
            echo -e "${GREEN}Stopping process $pid...${NC}"
            kill "$pid" 2>/dev/null || kill -9 "$pid" 2>/dev/null
        fi
    fi
done < "$SCRIPT_DIR/.pids"

# Remove PID file
rm -f "$SCRIPT_DIR/.pids"

# Also kill any remaining dotnet and node processes for this project
echo -e "${YELLOW}Cleaning up any remaining processes...${NC}"
pkill -f "Booksy.UserManagement.API" 2>/dev/null
pkill -f "Booksy.ServiceCatalog.Api" 2>/dev/null
pkill -f "Booksy.Gateway" 2>/dev/null
pkill -f "vite.*booksy-frontend" 2>/dev/null

echo ""
echo -e "${GREEN}All services stopped successfully!${NC}"
echo ""
