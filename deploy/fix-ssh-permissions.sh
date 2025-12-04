#!/bin/bash

# Quick script to fix SSH key permissions

SSH_KEY_PATH="${1:-$HOME/.ssh/id_rsa}"

if [ ! -f "$SSH_KEY_PATH" ]; then
    echo "SSH key not found at: $SSH_KEY_PATH"
    echo "Usage: $0 [path-to-ssh-key]"
    exit 1
fi

echo "Fixing permissions for: $SSH_KEY_PATH"
chmod 600 "$SSH_KEY_PATH"
echo "✅ Permissions fixed! (should be 600)"

# Verify
ls -l "$SSH_KEY_PATH"

