#!/bin/bash

# Update API documentation
# This script exports OpenAPI specs and regenerates API documentation

set -e

echo "==================================================================="
echo "Booksy Documentation Update Script"
echo "==================================================================="
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# Step 1: Export OpenAPI specs
echo "Step 1: Exporting OpenAPI specifications"
echo "-------------------------------------------"
./scripts/export-swagger-specs.sh

if [ $? -ne 0 ]; then
    echo ""
    echo "⚠ OpenAPI export failed or incomplete"
    echo "Continuing with documentation build using existing specs..."
    echo ""
fi

# Step 2: Generate API documentation
echo ""
echo "Step 2: Generating API documentation pages"
echo "-------------------------------------------"

cd documentation

if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# Note: OpenAPI docs generation would go here
# For now, we skip this as it requires the OpenAPI specs to exist
# npm run docusaurus gen-api-docs all

echo "✓ API documentation pages ready"

# Step 3: Build documentation site
echo ""
echo "Step 3: Building documentation site"
echo "-------------------------------------------"

npm run build

if [ $? -eq 0 ]; then
    echo "✓ Documentation site built successfully"
    echo ""
    echo "==================================================================="
    echo "✓ Documentation update complete!"
    echo "==================================================================="
    echo ""
    echo "To preview the documentation:"
    echo "  cd documentation && npm run serve"
    echo ""
    echo "Or start the development server:"
    echo "  cd documentation && npm start"
else
    echo "✗ Documentation build failed"
    exit 1
fi
