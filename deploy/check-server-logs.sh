#!/bin/bash

# Quick script to check server logs for errors

DROPLET_IP="159.65.242.79"

echo "📋 Recent server logs (last 50 lines):"
echo "======================================"
ssh root@$DROPLET_IP "journalctl -u mental-health-app -n 50 --no-pager" 2>/dev/null || echo "Could not connect"

echo ""
echo "📋 Errors in last 100 lines:"
echo "======================================"
ssh root@$DROPLET_IP "journalctl -u mental-health-app -n 100 --no-pager | grep -iE 'error|exception|fail|500' | tail -20" 2>/dev/null || echo "Could not connect"

