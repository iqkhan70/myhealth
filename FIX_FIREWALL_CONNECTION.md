# Fix: Server Not Reachable from Other Machine

## ✅ Good News

I can see the server **IS accepting connections** from other machines:
- Connection from `192.168.86.23` is established ✅
- Server is listening on `0.0.0.0:5262` ✅

## 🔧 The Issue: Firewall

The macOS firewall is likely blocking incoming connections. Here's how to fix it:

### Step 1: Check Firewall Status

Open **System Settings** → **Network** → **Firewall** (or **Security & Privacy** → **Firewall**)

Or run in terminal (will ask for password):
```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
```

### Step 2: Allow .NET Through Firewall

Run these commands (will ask for your password):

```bash
# Find the dotnet executable
which dotnet

# Usually it's at: /usr/local/share/dotnet/dotnet
# Or: /usr/local/bin/dotnet

# Add dotnet to firewall exceptions
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet
```

### Step 3: Or Temporarily Disable Firewall (Development Only)

**⚠️ Only for development!**

```bash
# Disable firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate off

# Re-enable later:
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate on
```

### Step 4: Test Connection

From the other machine:
```bash
# Test health endpoint (now available)
curl http://192.168.86.25:5262/api/health

# Or test any endpoint
curl http://192.168.86.25:5262/api/auth/login -X POST -H "Content-Type: application/json" -d '{}'
```

**Expected:**
- ✅ If firewall is fixed: You'll get a response (even 400/401 is OK - means server is reachable)
- ❌ If still blocked: Connection timeout or "refused"

## 🎯 Alternative: Use GUI

1. **Open System Settings** (or System Preferences on older macOS)
2. Go to **Network** → **Firewall** (or **Security & Privacy** → **Firewall**)
3. Click **Firewall Options** or **Options**
4. Click **+** to add an application
5. Navigate to: `/usr/local/share/dotnet/dotnet` (or wherever dotnet is installed)
6. Make sure it's set to **Allow incoming connections**

## 📝 Quick Test

**From the other machine, try:**
```bash
# Test if port is open
nc -zv 192.168.86.25 5262

# Should show:
# Connection to 192.168.86.25 port 5262 [tcp/*] succeeded!
```

If `nc` (netcat) shows "succeeded", the firewall is allowing it. If it shows "Connection refused" or timeout, firewall is blocking.

## ✅ I've Added a Health Endpoint

I created `/api/health` endpoint so you can test:
```bash
curl http://192.168.86.25:5262/api/health
```

Should return: `{"status":"healthy","timestamp":"..."}`

