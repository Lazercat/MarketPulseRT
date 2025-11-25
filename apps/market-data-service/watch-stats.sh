#!/bin/bash

# Simple script to watch market data stats in real-time
# Usage: ./watch-stats.sh

URL="${1:-http://localhost:5001/stats}"

echo "Watching stats from: $URL"
echo "Press Ctrl+C to exit"
echo ""

while true; do
    clear
    echo "Market Data Service - Real-time Stats"
    echo "======================================="
    echo ""

    if command -v jq &> /dev/null; then
        curl -s "$URL" | jq '.'
    else
        curl -s "$URL"
        echo ""
        echo "(Install 'jq' for prettier output: brew install jq)"
    fi

    echo ""
    echo "Last updated: $(date)"
    sleep 1
done
