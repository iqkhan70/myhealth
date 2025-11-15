#!/bin/bash
# Quick script to check Redis keys

echo "🔍 Checking Redis connection..."
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning PING

echo ""
echo "📋 All keys in Redis:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "*"

echo ""
echo "🎯 Agora token keys:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*"

echo ""
echo "📊 Key details:"
for key in $(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*"); do
    echo "---"
    echo "Key: $key"
    echo "TTL: $(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning TTL "$key") seconds"
    echo "Value (first 50 chars): $(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning GET "$key" | cut -c1-50)..."
done

echo ""
echo "✅ Done!"

