#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Quick script to check server logs for errors


echo "📋 Recent server logs (last 50 lines):"
echo "======================================"
ssh root@$DROPLET_IP "journalctl -u mental-health-app -n 50 --no-pager" 2>/dev/null || echo "Could not connect"

echo ""
echo "📋 Errors in last 100 lines:"
echo "======================================"
ssh root@$DROPLET_IP "journalctl -u mental-health-app -n 100 --no-pager | grep -iE 'error|exception|fail|500' | tail -20" 2>/dev/null || echo "Could not connect"

