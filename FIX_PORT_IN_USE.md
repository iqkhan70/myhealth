# Fix: Port 5262 Already in Use

## 🚨 The Error

```
Failed to bind to address http://0.0.0.0:5262: address already in use
```

This means another server instance is already running on port 5262.

## ✅ Quick Fix

### Option 1: Kill the Process (Recommended)

```bash
# Find the process
lsof -ti :5262

# Kill it (replace PID with the number from above)
kill -9 $(lsof -ti :5262)

# Or use the script I created:
./kill-server-port.sh
```

### Option 2: Find and Kill Manually

```bash
# Find what's using the port
lsof -i :5262

# Kill the process (look for PID in the output)
kill -9 <PID>
```

### Option 3: Use a Different Port

If you can't kill the process, use a different port:

**Update `launchSettings.json`:**
```json
"https": {
  "applicationUrl": "https://0.0.0.0:5263;http://0.0.0.0:5264"
}
```

**Update client `DependencyInjection.cs`** to use port 5263 for HTTPS.

## 🔍 Check if Port is Free

After killing the process:

```bash
# Should return nothing if port is free
lsof -i :5262
```

## 🚀 Then Start Server

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

## 📝 Common Causes

1. **Previous server instance still running** - Most common
2. **Another application using port 5262**
3. **Server crashed but port wasn't released** - Kill the process

## ✅ Quick Command

Just run this to kill whatever is on port 5262:

```bash
kill -9 $(lsof -ti :5262) 2>/dev/null || echo "Port 5262 is free"
```

Then start the server again!

