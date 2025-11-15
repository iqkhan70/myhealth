#!/bin/bash
# Debug script to check Redis connection and keys

echo "=========================================="
echo "Redis Debug Information"
echo "=========================================="
echo ""

echo "1. Checking Redis connection..."
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning PING
echo ""

echo "2. Current database (should be 0):"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning CLIENT INFO | grep db
echo ""

echo "3. Total keys in current database:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning DBSIZE
echo ""

echo "4. All keys in database 0:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning SELECT 0
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "*"
echo ""

echo "5. Agora token keys only:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*"
echo ""

echo "6. Checking all Redis databases (0-15):"
for db in {0..15}; do
    count=$(docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning -n $db DBSIZE 2>/dev/null)
    if [ "$count" != "0" ]; then
        echo "  Database $db: $count keys"
        docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning -n $db KEYS "*" 2>/dev/null
    fi
done
echo ""

echo "7. Redis server info:"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning INFO server | grep -E "redis_version|os|tcp_port"
echo ""

echo "=========================================="
echo "Done!"
echo "=========================================="

