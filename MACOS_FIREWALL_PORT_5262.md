# macOS Firewall: Allow Only Port 5262

## ⚠️ Important Note

**macOS Application Firewall works at the application level, not port level.**

This means you can't directly say "allow port 5262" - you need to allow the application (dotnet) that's listening on that port.

However, there are workarounds:

## ✅ Option 1: Allow .NET App (Recommended)

This is the simplest and recommended approach. macOS will allow the dotnet app to accept connections on any port it's listening on.

```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /opt/homebrew/bin/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /opt/homebrew/bin/dotnet
```

**Why this is fine:**
- The dotnet app only listens on ports you configure (5262 in your case)
- It's not a security risk - the app can only use ports you tell it to use
- Simpler to manage

## 🔧 Option 2: Use pfctl (Port-Based - Advanced)

If you really want port-based rules, you can use `pfctl` (packet filter), but it's more complex:

### Step 1: Disable Application Firewall

```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate off
```

### Step 2: Create pfctl Rule

Create a file `/etc/pf.anchors/allow_port_5262`:

```bash
sudo nano /etc/pf.anchors/allow_port_5262
```

Add this content:
```
# Allow incoming TCP connections on port 5262
pass in proto tcp from any to any port 5262
```

### Step 3: Load the Rule

```bash
# Load the anchor
sudo pfctl -f /etc/pf.anchors/allow_port_5262

# Enable packet filter
sudo pfctl -e
```

### Step 4: Verify

```bash
# Check pfctl status
sudo pfctl -s all
```

**⚠️ Warning:** This method:
- Disables Application Firewall (less user-friendly)
- Requires manual rule management
- More complex to troubleshoot
- May interfere with other network services

## 🎯 Recommended: Use Option 1

**Just allow the dotnet app** - it's simpler, safer, and works the same way:

```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /opt/homebrew/bin/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /opt/homebrew/bin/dotnet
```

The dotnet app will only listen on port 5262 (as configured in your `launchSettings.json`), so effectively you're only allowing that port.

## 📝 Why Application-Level is Better

1. **Simpler**: One command vs multiple steps
2. **Safer**: macOS manages it automatically
3. **User-friendly**: Shows in System Settings GUI
4. **Same result**: Only port 5262 is used by your server

## ✅ Quick Fix (Use This)

```bash
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /opt/homebrew/bin/dotnet
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /opt/homebrew/bin/dotnet
```

Then test:
```bash
# From other machine
curl http://192.168.86.25:5262/api/health
```

This effectively allows only port 5262 because that's the only port your dotnet server uses!

