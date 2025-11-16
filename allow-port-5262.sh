#!/bin/bash

# macOS Firewall: Allow Only Port 5262
# This script creates a firewall rule to allow incoming connections on port 5262

echo "🔧 macOS Firewall: Allow Port 5262 Only"
echo "========================================"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "❌ This script must be run with sudo"
    echo "   Run: sudo ./allow-port-5262.sh"
    exit 1
fi

# Check if pfctl is available
if ! command -v pfctl &> /dev/null; then
    echo "❌ pfctl not found. This method requires pfctl."
    echo "   Alternative: Use Application Firewall GUI method"
    exit 1
fi

echo "📝 Creating firewall rule for port 5262..."
echo ""

# Create a temporary pfctl rules file
RULES_FILE="/tmp/allow_port_5262.pf"

cat > "$RULES_FILE" << 'EOF'
# Allow incoming connections on port 5262
pass in proto tcp from any to any port 5262
EOF

echo "✅ Rule file created: $RULES_FILE"
echo ""
echo "⚠️  Note: macOS Application Firewall works at application level, not port level."
echo "   For port-based rules, we need to use pfctl (packet filter)."
echo ""
echo "📋 To apply this rule, you have two options:"
echo ""
echo "Option 1: Use Application Firewall (Recommended - Simpler)"
echo "   - Allow the dotnet app through firewall (GUI or command)"
echo "   - This allows dotnet to accept connections on any port"
echo ""
echo "Option 2: Use pfctl (Advanced - Port-specific)"
echo "   - Requires disabling Application Firewall"
echo "   - More complex to manage"
echo ""
echo "💡 Recommendation: Use Application Firewall method (allowing dotnet app)"
echo "   It's simpler and more secure for development."

