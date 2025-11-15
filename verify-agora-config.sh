#!/bin/bash

# Agora Configuration Verification Script
# This script checks if all Agora App IDs match across the codebase

APP_ID="efa11b3a7d05409ca979fb25a5b489ae"
CERT="89ab54068fae46aeaf930ffd493e977b"

echo "🔍 Verifying Agora Configuration..."
echo "=================================="
echo ""

# Check server files
echo "📋 Server Files:"
SERVER_COUNT=$(grep -r "$APP_ID" SM_MentalHealthApp.Server/ --include="*.cs" 2>/dev/null | wc -l | tr -d ' ')
echo "   Found App ID in $SERVER_COUNT server file(s)"

SERVER_CERT_COUNT=$(grep -r "$CERT" SM_MentalHealthApp.Server/ --include="*.cs" 2>/dev/null | wc -l | tr -d ' ')
echo "   Found App Certificate in $SERVER_CERT_COUNT server file(s)"

echo ""
echo "📋 Client Files:"
CLIENT_COUNT=$(grep -r "$APP_ID" SM_MentalHealthApp.Client/ --include="*.razor" --include="*.cs" 2>/dev/null | wc -l | tr -d ' ')
echo "   Found App ID in $CLIENT_COUNT client file(s)"

echo ""
echo "📋 All Files Using App ID:"
grep -r "$APP_ID" . --include="*.cs" --include="*.razor" --include="*.js" 2>/dev/null | grep -v "node_modules" | grep -v ".git" | cut -d: -f1 | sort -u

echo ""
echo "=================================="
echo "✅ Verification Complete"
echo ""
echo "📝 Next Steps:"
echo "1. Verify the App ID above matches your Agora Console"
echo "2. Check browser console when joining a call"
echo "3. Verify token generation returns the correct App ID"
echo ""

