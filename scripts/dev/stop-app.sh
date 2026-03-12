#!/bin/bash

# Stop DotNetStarterKitv2 - kill both API and Web servers
# Usage: ./scripts/dev/stop-app.sh

echo "🛑 Stopping DotNetStarterKitv2..."
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Kill dotnet processes
echo -e "${BLUE}[1/2]${NC} Stopping API servers..."
taskkill /F /IM dotnet.exe 2>/dev/null || pkill -f "dotnet run" || true
sleep 1
echo -e "${GREEN}✓ API stopped${NC}"

# Kill node processes (Vite)
echo -e "${BLUE}[2/2]${NC} Stopping Vite dev server..."
taskkill /F /IM node.exe 2>/dev/null || pkill -f "vite" || true
sleep 1
echo -e "${GREEN}✓ Web server stopped${NC}"

echo ""
echo -e "${GREEN}✓ All servers stopped${NC}"
