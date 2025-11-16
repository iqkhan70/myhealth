# Quick Fix: Connection Issues

## ✅ Fixed: Runtime Crash

The runtime crash is now fixed - added error handling to `handleActivity` function.

## 🔧 Fix Connection from Different Machine

### Your Server Status:
- ✅ Server is running on port 5262
- ✅ Listening on all interfaces (`0.0.0.0:5262`)
- ✅ Your IP: `192.168.86.25`

### From the Other Machine, Test:

1. **Test if server is reachable:**
   ```bash
   curl http://192.168.86.25:5262/api/health
   ```
   
   **Expected:**
   - ✅ If reachable: You'll get a response (even 404 is OK)
   - ❌ If not reachable: Connection timeout or "refused"

2. **Check firewall on server machine:**
   ```bash
   # Check firewall status
   sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
   
   # If firewall is ON, allow .NET:
   sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
   sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet
   ```

3. **Verify both machines are on same network:**
   - Both should be on same WiFi/LAN
   - IP addresses should be in same range (192.168.86.x)

### Common Issues:

#### Issue: "Connection refused"
**Cause:** Firewall blocking or server not listening on all interfaces
**Fix:** 
- Check firewall settings
- Verify `launchSettings.json` has `0.0.0.0`, not `localhost`

#### Issue: "Connection timeout"
**Cause:** Network connectivity issue
**Fix:**
- Verify both machines on same network
- Try pinging: `ping 192.168.86.25`

#### Issue: "Cannot connect"
**Cause:** Server might not be running
**Fix:**
- On server machine: `lsof -i :5262` (should show server process)
- Restart server: `dotnet run`

## 🎯 Quick Test

**From other machine, open browser:**
```
http://192.168.86.25:5262/api/health
```

**If you see ANY response (even 404), server is reachable!**

Then try the client login again.

