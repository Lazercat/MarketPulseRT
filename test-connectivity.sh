#!/bin/bash

echo "🔍 MarketPulseRT Connectivity Diagnostic"
echo "========================================"
echo ""

# Test 1: Market Data Service
echo "Test 1: Market Data Service (port 5001)"
echo "----------------------------------------"
if curl -s http://localhost:5001/test > /dev/null 2>&1; then
    echo "✅ Market Data Service is responding"
    curl -s http://localhost:5001/test | jq -r '.message' 2>/dev/null || curl -s http://localhost:5001/test
else
    echo "❌ Market Data Service is NOT responding"
fi
echo ""

# Test 2: API Gateway
echo "Test 2: API Gateway (port 5100)"
echo "-------------------------------"
if curl -s -I http://localhost:5100 > /dev/null 2>&1; then
    echo "✅ API Gateway is responding"
else
    echo "❌ API Gateway is NOT responding"
fi
echo ""

# Test 3: Check for gRPC errors in API Gateway
echo "Test 3: Check running API Gateway process"
echo "-----------------------------------------"
API_GW_PID=$(ps aux | grep "ApiGateway/bin/Debug" | grep -v grep | awk '{print $2}')
if [ -n "$API_GW_PID" ]; then
    echo "✅ API Gateway process found (PID: $API_GW_PID)"
    echo "   Started at: $(ps -p $API_GW_PID -o lstart= 2>/dev/null)"
else
    echo "❌ API Gateway process not found"
fi
echo ""

# Test 4: Check API Gateway version
echo "Test 4: Check API Gateway code timestamp"
echo "----------------------------------------"
if [ -f "apps/api-gateway/Services/MarketStreamerRelay.cs" ]; then
    if grep -q "HttpVersionPolicy" apps/api-gateway/Services/MarketStreamerRelay.cs; then
        echo "✅ HTTP/1.1 fix is present in code"
    else
        echo "❌ HTTP/1.1 fix is NOT in code"
    fi
    echo "   Last modified: $(stat -f "%Sm" apps/api-gateway/Services/MarketStreamerRelay.cs)"
else
    echo "❌ MarketStreamerRelay.cs not found"
fi
echo ""

# Test 5: Check if process needs restart
echo "Test 5: Process vs Code Comparison"
echo "----------------------------------"
CODE_MODIFIED=$(stat -f "%m" apps/api-gateway/Services/MarketStreamerRelay.cs 2>/dev/null)
if [ -n "$API_GW_PID" ] && [ -n "$CODE_MODIFIED" ]; then
    PROCESS_STARTED=$(ps -p $API_GW_PID -o lstart= 2>/dev/null | xargs -I {} date -j -f "%a %b %d %T %Y" "{}" "+%s" 2>/dev/null)

    if [ -n "$PROCESS_STARTED" ]; then
        if [ "$CODE_MODIFIED" -gt "$PROCESS_STARTED" ]; then
            echo "⚠️  CODE IS NEWER than running process - RESTART NEEDED!"
        else
            echo "✅ Running process has latest code"
        fi
    fi
fi
echo ""

# Test 6: Check ports
echo "Test 6: Port Status"
echo "------------------"
echo "Port 5001 (Market Data): $(lsof -i :5001 | grep LISTEN | awk '{print $1}' || echo 'Not listening')"
echo "Port 5100 (API Gateway): $(lsof -i :5100 | grep LISTEN | awk '{print $1}' || echo 'Not listening')"
echo ""

echo "========================================"
echo "💡 Recommendations:"
echo ""

if [ -z "$API_GW_PID" ]; then
    echo "  1. Start API Gateway: dotnet run --project apps/api-gateway/ApiGateway.csproj"
elif [ -n "$PROCESS_STARTED" ] && [ "$CODE_MODIFIED" -gt "$PROCESS_STARTED" ]; then
    echo "  1. RESTART API Gateway to load latest code:"
    echo "     - Press Ctrl+C in the API Gateway terminal"
    echo "     - Run: dotnet run --project apps/api-gateway/ApiGateway.csproj"
else
    echo "  1. Check API Gateway terminal for HTTP/2 errors"
    echo "  2. If you see repeated 'HTTP_1_1_REQUIRED' errors, restart the API Gateway"
fi

echo ""
echo "  Then test with: open signalr-tester.html"
