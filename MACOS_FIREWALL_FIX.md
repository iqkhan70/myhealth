# macOS Firewall: Allow .NET Server Connections

## 🎯 Quick Fix: Command Line (Fastest)

### Step 1: Find .NET Location
```bash
which dotnet
```

Common locations:
- `/usr/local/share/dotnet/dotnet`
- `/usr/local/bin/dotnet`
- `/opt/homebrew/bin/dotnet` (if using Homebrew)

### Step 2: Allow .NET Through Firewall

Run these commands (will ask for your password):

```bash
# Replace the path with your actual dotnet location from Step 1
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet
```

### Step 3: Verify It's Allowed

```bash
# Check firewall status
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# List allowed apps
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --listapps
```

You should see `dotnet` in the list.

### Step 4: Test Connection

From the other machine:
```bash
curl http://192.168.86.25:5262/api/health
```

## 🖥️ Alternative: GUI Method (Easier)

### Step 1: Open System Settings

1. Click **Apple menu** (🍎) → **System Settings** (or **System Preferences** on older macOS)
2. Go to **Network** → **Firewall** (or **Security & Privacy** → **Firewall**)

### Step 2: Configure Firewall

1. Click **Firewall Options** (or **Options** button)
2. If firewall is **Off**, turn it **On**
3. Make sure **"Block all incoming connections"** is **NOT checked**
4. Click **+** (plus button) to add an application
5. Navigate to: `/usr/local/share/dotnet/dotnet`
   - If not found, try: `/usr/local/bin/dotnet`
   - Or use **Go** → **Go to Folder** and paste the path
6. Make sure it's set to **"Allow incoming connections"**
7. Click **OK**

### Step 3: Restart Server

After allowing .NET, restart your server:
```bash
# Stop server (Ctrl+C)
cd SM_MentalHealthApp.Server
dotnet run
```

## 🔧 If Still Not Working

### Option 1: Temporarily Disable Firewall (Development Only)

**⚠️ Only for development! Re-enable after testing!**

```bash
# Disable firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate off

# Test connection from other machine
# If it works, firewall was the issue

# Re-enable firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate on

# Then add dotnet properly (see above)
```

### Option 2: Check Firewall Logs

```bash
# View firewall logs
sudo log show --predicate 'process == "socketfilterfw"' --last 5m
```

### Option 3: Allow Specific Port

If the above doesn't work, you can allow the specific port:

```bash
# Allow port 5262
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /System/Library/PrivateFrameworks/NetworkServiceProxy.framework/Support/network_service_proxy
sudo pfctl -d  # Disable packet filter (advanced, not recommended)
```

**Better approach:** Just allow the dotnet app (Option 1 or GUI method above).

## ✅ Verification

### Test 1: From Server Machine
```bash
curl http://localhost:5262/api/health
```
Should work ✅

### Test 2: From Other Machine
```bash
curl http://192.168.86.25:5262/api/health
```
Should work after firewall fix ✅

### Test 3: Check Port is Open
```bash
# From other machine
nc -zv 192.168.86.25 5262
```

**Expected:**
- ✅ `Connection to 192.168.86.25 port 5262 [tcp/*] succeeded!` → Firewall is allowing it
- ❌ `Connection refused` or timeout → Firewall is still blocking

## 📝 Complete Command Sequence

Here's the complete sequence to run:

```bash
# 1. Find dotnet
which dotnet

# 2. Add to firewall (replace path if different)
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet

# 3. Verify
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --listapps | grep dotnet

# 4. Test (from other machine)
curl http://192.168.86.25:5262/api/health
```

## 🎯 Most Common Solution

**Just run these two commands:**

```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /usr/local/share/dotnet/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /usr/local/share/dotnet/dotnet
```

Then test from the other machine. This should fix it!

