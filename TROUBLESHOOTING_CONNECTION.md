# Troubleshooting Connection Issues

## 🚨 Issue 1: Runtime Crash

**Error:**
```
Assert failed: .NET runtime already exited with 1
ERR18: expected ws instance
```

**Fixed:** Added try-catch around `handleActivity` to prevent crashes when runtime is shutting down.

## 🚨 Issue 2: Cannot Connect to Server from Different Machine

**Error:**
```
Cannot connect to server. Please check if the server is running at http://macip:5262
```

### ✅ Step-by-Step Troubleshooting

#### 1. **Verify Server is Running**

On the server machine:
```bash
# Check if server is running
lsof -i :5262

# Should show something like:
# dotnet  12345  user   23u  IPv4  TCP  *:5262 (LISTEN)
```

#### 2. **Verify Server is Listening on All Interfaces**

Check `launchSettings.json`:
```json
"applicationUrl": "http://0.0.0.0:5262"
```

✅ `0.0.0.0` means "listen on all network interfaces" - this is correct!

❌ `localhost` or `127.0.0.1` means "only listen on localhost" - won't work from other machines

#### 3. **Check Firewall**

**On macOS:**
```bash
# Check firewall status
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# If firewall is on, allow .NET:
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet
```

**Or temporarily disable firewall:**
```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate off
```

#### 4. **Test Server from Different Machine**

From the other machine, try:
```bash
# Test if server is reachable
curl http://MAC_IP:5262/api/health

# Or in browser:
http://MAC_IP:5262/api/health
```

**Expected:**
- ✅ If server is reachable: You'll get a response (even if 404)
- ❌ If server is not reachable: Connection timeout or "refused"

#### 5. **Check Network Connectivity**

**On server machine:**
```bash
# Find your IP address
ifconfig | grep "inet " | grep -v 127.0.0.1

# Should show something like:
# inet 192.168.86.25 netmask 0xffffff00 broadcast 192.168.86.255
```

**Verify both machines are on same network:**
- Both should be on same WiFi/LAN
- IP addresses should be in same range (e.g., 192.168.86.x)

#### 6. **Check Server Logs**

When you start the server, you should see:
```
Now listening on: http://0.0.0.0:5262
```

If you see `localhost` instead of `0.0.0.0`, that's the problem!

#### 7. **Test with Different Port**

If port 5262 is blocked, try a different port:

**Update `launchSettings.json`:**
```json
"applicationUrl": "http://0.0.0.0:5000"
```

**Update client `DependencyInjection.cs`:**
```csharp
var serverUrl = $"http://{baseUri.Host}:5000/";
```

## 🔧 Quick Fixes

### Fix 1: Restart Server
```bash
# Stop server (Ctrl+C)
# Then restart:
cd SM_MentalHealthApp.Server
dotnet run
```

### Fix 2: Check Server is Actually Running
```bash
# On server machine:
netstat -an | grep 5262

# Should show:
# tcp4  0  0  *.5262  *.*  LISTEN
```

### Fix 3: Test from Server Machine Itself
```bash
# On server machine, test locally:
curl http://localhost:5262/api/health

# Then test with IP:
curl http://192.168.86.25:5262/api/health
```

If localhost works but IP doesn't, it's a firewall issue.

## 📝 Common Issues

### Issue: Server shows "localhost" instead of "0.0.0.0"
**Fix:** Make sure `launchSettings.json` has `0.0.0.0`, not `localhost`

### Issue: Firewall blocking
**Fix:** Allow .NET through firewall or temporarily disable

### Issue: Wrong IP address
**Fix:** Use `ifconfig` to find correct IP, make sure both machines are on same network

### Issue: Server not started
**Fix:** Make sure server is actually running - check with `lsof -i :5262`

## 🎯 Verification Checklist

- [ ] Server is running (`lsof -i :5262` shows process)
- [ ] Server is listening on `0.0.0.0:5262` (not `localhost`)
- [ ] Firewall allows port 5262
- [ ] Both machines on same network
- [ ] IP address is correct
- [ ] Can reach server from other machine (`curl http://IP:5262`)

