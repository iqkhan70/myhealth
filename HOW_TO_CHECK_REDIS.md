# How to Check Redis Keys - Step by Step

## 🎯 The Problem

You're connecting to Redis but seeing no keys. Here's why and how to fix it.

## ✅ Correct Way to Check Redis

### Option 1: Interactive Mode (Recommended)

```bash
# Step 1: Connect to Redis
docker exec -it redis_server redis-cli -a "StrongPassword123!"

# Step 2: Once connected, you'll see: 127.0.0.1:6379>
# Make sure you're in database 0 (default)
SELECT 0

# Step 3: Check how many keys exist
DBSIZE
# Should show a number > 0 if keys exist

# Step 4: List all keys
KEYS *

# Step 5: List only Agora token keys
KEYS agora_token:*

# Step 6: Get a specific token value
GET agora_token:call_2_3:2

# Step 7: Check expiration time
TTL agora_token:call_2_3:2
# Shows seconds until expiration, or -2 if key doesn't exist

# Step 8: Exit
exit
```

### Option 2: One-Liner Commands

```bash
# Check total keys
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning DBSIZE

# List all keys
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "*"

# List Agora tokens
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*"
```

### Option 3: Use the Script

```bash
./test-redis-connection.sh
```

## 🔍 Why You Might See No Keys

### 1. **Keys Expired**
Tokens expire after 1 hour (3600 seconds). If you wait too long, they'll be gone.

**Solution**: Make a call, then immediately check Redis:
```bash
# Make a call from your app, then immediately run:
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "*"
```

### 2. **Wrong Database**
Redis has 16 databases (0-15). Your app uses database 0.

**Solution**: Always run `SELECT 0` first:
```bash
docker exec -it redis_server redis-cli -a "StrongPassword123!"
SELECT 0
KEYS *
```

### 3. **No Tokens Generated Yet**
If you haven't made any calls, there won't be any tokens.

**Solution**: Make a test call first, then check Redis.

### 4. **Different Redis Instance**
You might be connecting to a different Redis than your app uses.

**Solution**: Check which Redis container your app connects to:
```bash
docker ps | grep redis
# Make sure you connect to the same one (usually redis_server)
```

## 🧪 Test Token Generation

To verify tokens are being generated:

1. **Make a call** from your app
2. **Immediately check Redis**:
   ```bash
   docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning KEYS "agora_token:*"
   ```
3. **You should see** keys like:
   - `agora_token:call_1_2:1`
   - `agora_token:call_3_5:3`
   - etc.

## 📊 Understanding the Output

When you run `KEYS *`, you should see:
```
1) "agora_token:call_2_3:2"
2) "agora_token:call_1_5:1"
```

If you see **nothing** (empty), it means:
- ✅ Redis is connected (no error = connection works)
- ❌ No keys exist (either expired or not generated yet)

## 🔧 Quick Troubleshooting

```bash
# 1. Check if Redis is running
docker ps | grep redis

# 2. Check connection
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning PING
# Should return: PONG

# 3. Check database 0
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning -n 0 DBSIZE
# Should show number of keys

# 4. List keys in database 0
docker exec redis_server redis-cli -a "StrongPassword123!" --no-auth-warning -n 0 KEYS "*"
```

## 💡 Pro Tip

If you want to see keys in real-time as they're created:

```bash
# Terminal 1: Monitor Redis
docker exec -it redis_server redis-cli -a "StrongPassword123!" MONITOR

# Terminal 2: Make a call from your app
# You'll see the SET command in Terminal 1 when token is cached
```

