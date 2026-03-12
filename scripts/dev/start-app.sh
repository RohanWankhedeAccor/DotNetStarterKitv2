#!/bin/bash

# Start DotNetStarterKitv2 - both API and Web servers
# Usage: ./scripts/dev/start-app.sh

echo "🚀 Starting DotNetStarterKitv2..."
echo ""

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"

echo -e "${BLUE}[1/2]${NC} Starting ASP.NET Core API on https://localhost:5001..."
cd "$PROJECT_ROOT"
dotnet build src/Api/Api.csproj > /dev/null 2>&1
dotnet run --project src/Api --no-build > /tmp/dotnet-api.log 2>&1 &
API_PID=$!
echo "      API PID: $API_PID"
sleep 2

echo -e "${BLUE}[2/2]${NC} Starting React Vite dev server on http://localhost:5173..."
cd "$PROJECT_ROOT/src/Web"
npm run dev > /tmp/vite-dev.log 2>&1 &
WEB_PID=$!
echo "      Web PID: $WEB_PID"
sleep 3

echo ""
echo -e "${GREEN}✓ App is starting!${NC}"
echo ""
echo "📍 API:  https://localhost:5001"
echo "📍 Web:  http://localhost:5173"
echo ""
echo "Process IDs:"
echo "  API: $API_PID"
echo "  Web: $WEB_PID"
echo ""
echo "To stop: ./scripts/dev/stop-app.sh"
echo "API logs: tail -f /tmp/dotnet-api.log"
echo "Web logs: tail -f /tmp/vite-dev.log"
