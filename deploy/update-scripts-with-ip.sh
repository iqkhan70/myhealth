#!/bin/bash

# Script to update all deployment scripts to use centralized DROPLET_IP
# This script replaces hardcoded IPs with source to load-droplet-ip.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP"

if [ ! -f "$DROPLET_IP_FILE" ]; then
    echo "Error: DROPLET_IP file not found. Please create it first."
    exit 1
fi

DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')
echo "Updating scripts to use DROPLET_IP from file: $DROPLET_IP"

# Find all shell scripts in deploy directory that contain the IP
find "$SCRIPT_DIR" -name "*.sh" -type f | while read script; do
    if grep -q "159.65.242.79\|DROPLET_IP=\"159.65.242.79\"\|SERVER_IP=\"159.65.242.79\"" "$script" 2>/dev/null; then
        echo "Found IP in: $script"
        # Add source line at the top if not present
        if ! grep -q "load-droplet-ip.sh" "$script"; then
            # Find the first line that's not a shebang or comment
            sed -i.bak "1a\\
# Load centralized DROPLET_IP\\
source \"\$(cd \"\$(dirname \"\${BASH_SOURCE[0]}\")\" && pwd)/load-droplet-ip.sh\"
" "$script"
            # Remove backup file
            rm -f "${script}.bak"
        fi
        # Replace hardcoded IPs with variable
        sed -i.bak "s/159\.65\.242\.79/\${DROPLET_IP}/g" "$script"
        sed -i.bak "s/DROPLET_IP=\"159\.65\.242\.79\"/# DROPLET_IP loaded from load-droplet-ip.sh/g" "$script"
        sed -i.bak "s/SERVER_IP=\"159\.65\.242\.79\"/# SERVER_IP loaded from load-droplet-ip.sh/g" "$script"
        rm -f "${script}.bak"
    fi
done

echo "✅ Scripts updated. Review changes before committing."

