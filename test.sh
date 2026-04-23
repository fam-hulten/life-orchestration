#!/bin/bash
# Test script for life-orchestration
# Usage: ./test.sh

set -e

echo "🔧 Building solution..."
dotnet build

echo ""
echo "🧪 Running tests..."
dotnet test --verbosity normal

echo ""
echo "✅ All tests passed!"