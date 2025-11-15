# Redis Connection Guide

## 🔍 Why You Might Not See Keys

### 1. **Database Selection**
Redis has 16 databases (0-15). Make sure you're in database 0:
```bash
docker exec -it redis_server redis-cli -a "StrongPassword123!"
# Then type:
SELECT 0
KEYS *
```

### 2. **Keys Expired**
Tokens expire after 1 hour. If you wait too long, they'll be gone:
```bash
# Check TTL (Time To Live) - shows seconds until expiration
TTL agora_token:call_2_3:2
# -2 = key doesn't exist
# -1 = key exists but has no expiration
# >0 = seconds until expiration
```

### 3. **Different Redis Instance**
Make sure you're connecting to the same Redis that your .NET server uses:
```bash
# Check which Redis container is running
docker ps | grep redis

# Connect to the correct one (usually redis_server)
docker exec -it redis_server redis-cli -a "StrongPassword123!"
```

## ✅ Correct Way to Connect

```bash
# Step 1: Connect to Redis
docker exec -it redis_server redis-cli -a "StrongPassword123!"

# Step 2: Select database 0 (default)
SELECT 0

# Step 3: List all keys
KEYS *

# Step 4: List Agora tokens only
KEYS agora_token:*

# Step 5: Get a specific token value
GET agora_token:call_2_3:2

# Step 6: Check expiration
TTL agora_token:call_2_3:2
```

## 🐛 Quick Debug Commands

```bash
# Run the debug script
./debug-redis.sh

# Or manually:
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning SELECT 0
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "*"
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning DBSIZE
```

## 📝 What You Should See

When you run `KEYS *`, you should see keys like:
- `agora_token:call_2_3:2`
- `agora_token:call_1_5:1`
- etc.

If you see **nothing**, it means:
1. No tokens have been generated yet (make a call first)
2. All tokens expired (they expire after 1 hour)
3. You're in the wrong database (use `SELECT 0`)

## 🔧 If Still No Keys

1. **Make a test call** - This will generate a token and cache it
2. **Check immediately** - Run `KEYS *` right after making a call
3. **Check server logs** - Look for "Generated and cached new token" messages

