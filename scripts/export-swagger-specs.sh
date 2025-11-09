#!/bin/bash

# Export OpenAPI specifications for documentation site
# This script downloads OpenAPI specs from running API instances

set -e

echo "==================================================================="
echo "Booksy OpenAPI Specification Export Script"
echo "==================================================================="
echo ""

# Configuration
USERMGMT_URL="${USERMGMT_API_URL:-http://localhost:5000}"
SERVICECAT_URL="${SERVICECAT_API_URL:-http://localhost:5010}"
OUTPUT_DIR="documentation/static/openapi"

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo "Configuration:"
echo "  UserManagement API: $USERMGMT_URL"
echo "  ServiceCatalog API: $SERVICECAT_URL"
echo "  Output Directory: $OUTPUT_DIR"
echo ""

# Function to check if API is running
check_api() {
    local url=$1
    local name=$2

    echo -n "Checking $name at $url... "

    if curl -s -f -o /dev/null "$url/health" 2>/dev/null; then
        echo "✓ Running"
        return 0
    else
        echo "✗ Not accessible"
        return 1
    fi
}

# Function to download OpenAPI spec
download_spec() {
    local url=$1
    local version=$2
    local output_file=$3
    local api_name=$4

    echo -n "Downloading $api_name v$version OpenAPI spec... "

    if curl -s -f "$url/swagger/v$version/swagger.json" -o "$output_file" 2>/dev/null; then
        # Verify the file is valid JSON
        if jq empty "$output_file" 2>/dev/null; then
            local size=$(du -h "$output_file" | cut -f1)
            echo "✓ Success ($size)"
            return 0
        else
            echo "✗ Failed (Invalid JSON)"
            rm -f "$output_file"
            return 1
        fi
    else
        echo "✗ Failed (Download error)"
        return 1
    fi
}

# Check if APIs are running
echo "Step 1: Checking API availability"
echo "-----------------------------------"

USERMGMT_RUNNING=false
SERVICECAT_RUNNING=false

if check_api "$USERMGMT_URL" "UserManagement API"; then
    USERMGMT_RUNNING=true
fi

if check_api "$SERVICECAT_URL" "ServiceCatalog API"; then
    SERVICECAT_RUNNING=true
fi

echo ""

# Download specifications
echo "Step 2: Downloading OpenAPI specifications"
echo "-------------------------------------------"

SUCCESS_COUNT=0
TOTAL_COUNT=0

if [ "$USERMGMT_RUNNING" = true ]; then
    ((TOTAL_COUNT++))
    if download_spec "$USERMGMT_URL" "1" "$OUTPUT_DIR/usermanagement-v1.json" "UserManagement"; then
        ((SUCCESS_COUNT++))
    fi
fi

if [ "$SERVICECAT_RUNNING" = true ]; then
    ((TOTAL_COUNT++))
    if download_spec "$SERVICECAT_URL" "1" "$OUTPUT_DIR/servicecatalog-v1.json" "ServiceCatalog"; then
        ((SUCCESS_COUNT++))
    fi
fi

echo ""

# Summary
echo "==================================================================="
echo "Summary"
echo "==================================================================="
echo "Successfully downloaded: $SUCCESS_COUNT/$TOTAL_COUNT specifications"
echo "Output directory: $OUTPUT_DIR"
echo ""

if [ "$SUCCESS_COUNT" -eq "$TOTAL_COUNT" ] && [ "$TOTAL_COUNT" -gt 0 ]; then
    echo "✓ All OpenAPI specs exported successfully!"
    echo ""
    echo "Next steps:"
    echo "  1. Review the exported files in $OUTPUT_DIR"
    echo "  2. Run 'cd documentation && npm run build' to rebuild docs"
    echo "  3. Run 'cd documentation && npm start' to preview locally"
    exit 0
elif [ "$SUCCESS_COUNT" -gt 0 ]; then
    echo "⚠ Partial success - some specs could not be downloaded"
    echo ""
    echo "Troubleshooting:"
    echo "  - Ensure APIs are running: docker-compose up"
    echo "  - Check API URLs are correct"
    echo "  - Verify Swagger is enabled in development mode"
    exit 1
else
    echo "✗ Failed to download any OpenAPI specifications"
    echo ""
    echo "Please ensure:"
    echo "  1. APIs are running: docker-compose up"
    echo "     OR"
    echo "     cd src/UserManagement/Booksy.UserManagement.API && dotnet run"
    echo "     cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api && dotnet run"
    echo ""
    echo "  2. Swagger endpoints are accessible:"
    echo "     curl $USERMGMT_URL/swagger/v1/swagger.json"
    echo "     curl $SERVICECAT_URL/swagger/v1/swagger.json"
    exit 1
fi
