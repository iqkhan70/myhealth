#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

echo "🔍 Diagnosing 502 Bad Gateway issue..."
echo ""

SSH_KEY="$HOME/.ssh/id_rsa"

echo "1. Checking if backend service is running..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "systemctl status mental-health-app --no-pager | head -20"
echo ""

echo "2. Checking if backend is listening on port 5262..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "ss -tlnp | grep 5262"
echo ""

echo "3. Checking backend service logs (last 30 lines)..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "journalctl -u mental-health-app --no-pager -n 30"
echo ""

echo "4. Testing backend directly (from server)..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "curl -k https://localhost:5262/api/health 2>&1"
echo ""

echo "5. Checking nginx error logs..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "tail -20 /var/log/nginx/error.log"
echo ""

echo "6. Checking nginx configuration..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "nginx -t 2>&1"
echo ""

echo "7. Checking if certificates exist..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "ls -la /opt/mental-health-app/certs/ 2>&1"
echo ""

echo "8. Checking service file configuration..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "cat /etc/systemd/system/mental-health-app.service"
echo ""

echo "9. Checking if dotnet is accessible..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "which dotnet && dotnet --version"
echo ""

echo "10. Checking if DLL exists..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP "ls -la /opt/mental-health-app/server/SM_MentalHealthApp.Server.dll"
echo ""

echo "✅ Diagnosis complete. Review the output above to identify the issue."

