#!/bin/bash

# Script to update all deployment scripts to use centralized DROPLET_IP
# This replaces hardcoded IPs with source to load-droplet-ip.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "🔄 Updating all deployment scripts to use centralized DROPLET_IP..."
echo ""

# Find all shell scripts that have hardcoded IP
find "$SCRIPT_DIR" -name "*.sh" -type f | while read script; do
    # Skip this script and helper scripts
    if [[ "$script" == *"update-all-scripts-ip.sh" ]] || \
       [[ "$script" == *"load-droplet-ip.sh" ]] || \
       [[ "$script" == *"update-"*".sh" ]]; then
        continue
    fi
    
    # Check if script has hardcoded IP
    if grep -qE 'DROPLET_IP="159\.65\.242\.79"|SERVER_IP="159\.65\.242\.79"|159\.65\.242\.79' "$script" 2>/dev/null; then
        echo "📝 Updating: $(basename "$script")"
        
        # Create backup
        cp "$script" "${script}.bak"
        
        # Remove old DROPLET_IP or SERVER_IP lines
        sed -i.tmp '/^DROPLET_IP="159\.65\.242\.79"$/d' "$script"
        sed -i.tmp '/^SERVER_IP="159\.65\.242\.79"$/d' "$script"
        rm -f "${script}.tmp"
        
        # Add source line after shebang if not present
        if ! grep -q "load-droplet-ip.sh" "$script"; then
            # Find line after shebang (usually line 2 or 3)
            if head -1 "$script" | grep -q "^#!/bin/bash"; then
                # Insert after shebang
                sed -i.tmp '1a\
# Load centralized DROPLET_IP\
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
' "$script"
                rm -f "${script}.tmp"
            fi
        fi
        
        # Replace remaining hardcoded IPs with variable
        sed -i.tmp "s/159\.65\.242\.79/\${DROPLET_IP}/g" "$script"
        rm -f "${script}.tmp"
        
        echo "   ✅ Updated"
    fi
done

echo ""
echo "✅ All scripts updated!"
echo ""
echo "📋 Review changes:"
echo "   git diff deploy/"
echo ""
echo "💡 If changes look good, commit them. Otherwise restore:"
echo "   find deploy/ -name '*.bak' -exec sh -c 'mv \"\$1\" \"\${1%.bak}\"' _ {} \;"

