#!/bin/bash

# Script to view server logs easily

DROPLET_IP="159.65.242.79"
SERVICE_NAME="mental-health-app"

echo "📋 Server Logs for $SERVICE_NAME"
echo "=================================="
echo ""
echo "Choose an option:"
echo "1. Last 50 lines (quick view)"
echo "2. Last 100 lines"
echo "3. Follow logs in real-time (Ctrl+C to exit)"
echo "4. Errors only (last 100 lines)"
echo "5. Last 10 minutes"
echo ""
read -p "Enter option (1-5): " choice

case $choice in
    1)
        echo "📋 Last 50 lines:"
        ssh root@$DROPLET_IP "journalctl -u $SERVICE_NAME -n 50 --no-pager"
        ;;
    2)
        echo "📋 Last 100 lines:"
        ssh root@$DROPLET_IP "journalctl -u $SERVICE_NAME -n 100 --no-pager"
        ;;
    3)
        echo "📋 Following logs (Ctrl+C to exit):"
        ssh root@$DROPLET_IP "journalctl -u $SERVICE_NAME -f"
        ;;
    4)
        echo "📋 Errors only:"
        ssh root@$DROPLET_IP "journalctl -u $SERVICE_NAME -n 100 --no-pager | grep -iE 'error|exception|fail|500|401'"
        ;;
    5)
        echo "📋 Last 10 minutes:"
        ssh root@$DROPLET_IP "journalctl -u $SERVICE_NAME --since '10 minutes ago' --no-pager"
        ;;
    *)
        echo "Invalid option"
        exit 1
        ;;
esac

