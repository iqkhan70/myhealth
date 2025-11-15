#!/bin/bash
# Simple script to test Redis connection and show keys

echo "Connecting to Redis..."
echo ""

# Connect and show keys
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning <<EOF
SELECT 0
DBSIZE
KEYS *
KEYS agora_token:*
EOF

echo ""
echo "If you see no keys, it means:"
echo "1. No tokens have been generated yet (make a call first)"
echo "2. All tokens expired (they expire after 1 hour)"
echo "3. You're checking the wrong Redis instance"

