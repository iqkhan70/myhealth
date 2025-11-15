#!/bin/bash
# Script to clear all Agora token keys from Redis

echo "=========================================="
echo "Clearing Agora Token Keys from Redis"
echo "=========================================="
echo ""

# Connect to Redis and delete all agora_token keys
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning <<EOF
SELECT 0
KEYS agora_token:*
EOF

echo ""
echo "Deleting keys..."
KEYS_TO_DELETE=$(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*" 2>/dev/null)

if [ -z "$KEYS_TO_DELETE" ]; then
    echo "✅ No keys to delete"
else
    for key in $KEYS_TO_DELETE; do
        docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning DEL "$key" > /dev/null 2>&1
        echo "  Deleted: $key"
    done
    echo ""
    echo "✅ All Agora token keys cleared!"
fi

echo ""
echo "Verifying..."
REMAINING=$(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*" 2>/dev/null)
if [ -z "$REMAINING" ]; then
    echo "✅ Confirmed: No Agora token keys remaining"
else
    echo "⚠️  Warning: Some keys still exist:"
    echo "$REMAINING"
fi

echo ""
echo "=========================================="
echo "Done!"
echo "=========================================="

