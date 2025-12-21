#!/bin/bash

# Script to diagnose 401 Unauthorized issue on DigitalOcean server

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

echo "🔍 Diagnosing 401 Unauthorized Issue..."
echo "=========================================="
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "1️⃣ Checking server appsettings.Production.json..."
echo "---------------------------------------------------"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "cat /opt/mental-health-app/server/appsettings.Production.json 2>/dev/null | grep -A 5 'Jwt' || echo '⚠️ JWT section not found or file missing'"
echo ""

echo "2️⃣ Checking server appsettings.json..."
echo "---------------------------------------------------"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "cat /opt/mental-health-app/server/appsettings.json 2>/dev/null | grep -A 5 'Jwt' || echo '⚠️ JWT section not found or file missing'"
echo ""

echo "3️⃣ Checking ASPNETCORE_ENVIRONMENT..."
echo "---------------------------------------------------"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl show mental-health-app | grep Environment || echo '⚠️ Environment variable not found'"
echo ""

echo "4️⃣ Checking recent server logs for auth errors..."
echo "---------------------------------------------------"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "journalctl -u mental-health-app -n 100 --no-pager | grep -iE '401|unauthorized|jwt|auth|token' | tail -20 || echo 'No auth-related errors found'"
echo ""

echo "5️⃣ Checking CORS configuration in DependencyInjection.cs..."
echo "---------------------------------------------------"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "grep -A 10 'WithOrigins' /opt/mental-health-app/server/DependencyInjection.cs 2>/dev/null | head -15 || echo '⚠️ File not found or CORS config not found'"
echo ""

echo "6️⃣ Testing if server is accessible..."
echo "---------------------------------------------------"
curl -k -s -o /dev/null -w "HTTP Status: %{http_code}\n" "https://$DROPLET_IP/api/health" || echo "❌ Cannot reach server"
echo ""

echo "✅ Diagnosis complete!"
echo ""
echo "📋 Next steps:"
echo "   1. Compare JWT keys between local and server"
echo "   2. Ensure server's appsettings.Production.json has correct JWT key"
echo "   3. Check if CORS allows the client origin"
echo "   4. Verify ASPNETCORE_ENVIRONMENT is set correctly"

