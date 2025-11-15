# Redis Connection & Commands Guide

## 🔌 Connect to Redis

### Option 1: Using redis-cli (if installed locally)
```bash
redis-cli -h localhost -p 6379 -a "StrongPassword123!"
```

### Option 2: Using Docker (if Redis is in Docker)
```bash
docker exec -it <redis-container-name> redis-cli -a "StrongPassword123!"
```

### Option 3: Find Redis container and connect
```bash
# Find Redis container
docker ps | grep redis

# Connect (replace <container-id> with actual ID)
docker exec -it <container-id> redis-cli -a "StrongPassword123!"
```

## 🔑 Useful Redis Commands

### Check Connection
```bash
PING
# Should return: PONG
```

### List ALL Keys
```bash
KEYS *
```

### List Agora Token Keys Only
```bash
KEYS agora_token:*
```

### Get Value of a Specific Key
```bash
GET agora_token:call_1_2:1
```

### Check if Key Exists
```bash
EXISTS agora_token:call_1_2:1
# Returns: 1 (exists) or 0 (doesn't exist)
```

### Check Time to Live (TTL) - How long until key expires
```bash
TTL agora_token:call_1_2:1
# Returns: seconds until expiration, or -1 (no expiration), or -2 (key doesn't exist)
```

### Get All Keys Matching Pattern (with values)
```bash
# Get all Agora token keys and their values
KEYS agora_token:* | xargs -I {} redis-cli -a "StrongPassword123!" GET {}
```

### Delete a Specific Key
```bash
DEL agora_token:call_1_2:1
```

### Delete ALL Agora Token Keys
```bash
# ⚠️ WARNING: This deletes all cached tokens
KEYS agora_token:* | xargs redis-cli -a "StrongPassword123!" DEL
```

### Get Key Type
```bash
TYPE agora_token:call_1_2:1
# Should return: string
```

### Get Database Info
```bash
INFO keyspace
# Shows number of keys in database
```

## 📊 Monitor Redis in Real-Time

### Watch all commands
```bash
MONITOR
# Press Ctrl+C to stop
```

### Watch specific pattern
```bash
# In one terminal, start monitor
redis-cli -h localhost -p 6379 -a "StrongPassword123!" MONITOR | grep agora_token
```

## 🔍 Example: Check Agora Token Cache

```bash
# 1. Connect
redis-cli -h localhost -p 6379 -a "StrongPassword123!"

# 2. List all Agora token keys
KEYS agora_token:*

# 3. Get a specific token value
GET agora_token:call_1_2:1

# 4. Check expiration time
TTL agora_token:call_1_2:1

# 5. Exit
exit
```

## 🐳 If Using Docker

### Find Redis Container
```bash
docker ps | grep redis
```

### Connect via Docker
```bash
docker exec -it <container-name-or-id> redis-cli -a "StrongPassword123!"
```

### Or use docker-compose (if applicable)
```bash
docker-compose exec redis redis-cli -a "StrongPassword123!"
```

## 📝 Quick One-Liners

### Count Agora tokens
```bash
redis-cli -h localhost -p 6379 -a "StrongPassword123!" --raw KEYS "agora_token:*" | wc -l
```

### List all keys with their TTL
```bash
redis-cli -h localhost -p 6379 -a "StrongPassword123!" --scan --pattern "agora_token:*" | while read key; do echo "$key: $(redis-cli -h localhost -p 6379 -a "StrongPassword123!" TTL "$key")"; done
```

### Get all Agora tokens (keys and values)
```bash
redis-cli -h localhost -p 6379 -a "StrongPassword123!" --scan --pattern "agora_token:*" | while read key; do echo "=== $key ==="; redis-cli -h localhost -p 6379 -a "StrongPassword123!" GET "$key"; echo ""; done
```

