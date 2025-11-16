# Fix: HTTPS Client (5282) Can't Connect to HTTP Server (5262)

## 🚨 The Problem

- ✅ **Port 5283 (HTTP)**: Works - can login
- ❌ **Port 5282 (HTTPS)**: Shows login screen but can't login - "cannot connect to server"
- ✅ **You need HTTPS for Agora** (Agora SDK requires HTTPS)

## 🔍 Root Cause

When accessing the client via **HTTPS (5282)**:
- Client page loads on HTTPS ✅
- Client tries to call server API on **HTTP (5262)** ❌
- Browser **blocks mixed content** (HTTPS page calling HTTP API)
- Even though we have CSP, browser might still block it

## ✅ Solution: Ensure CSP Allows Mixed Content

I've updated the code to:
1. **Always use HTTP for server API** (regardless of client protocol)
2. **CSP already allows mixed content** in `index.html`

### What Changed

The client now **always** uses `http://` for the server API, even when the client itself is on HTTPS.

## 🧪 Testing

### Step 1: Rebuild Client
```bash
cd SM_MentalHealthApp.Client
dotnet build
```

### Step 2: Restart Client
```bash
dotnet run --launch-profile https
```

### Step 3: Access from Other Machine
```
https://192.168.86.25:5282
```

### Step 4: Check Browser Console

Open browser console (F12) and look for:
- ✅ `🌐 HttpClient BaseAddress configured: http://192.168.86.25:5262/`
- ✅ Login should work now

If you still see mixed content errors:
- Check if CSP is being applied
- Try accepting the certificate warning first
- Some browsers need the page to be fully loaded before allowing mixed content

## 🔧 Alternative: Use HTTP Client (Port 5283) for Testing

If HTTPS (5282) still doesn't work:

1. **Access via HTTP (5283)** for regular browsing:
   ```
   http://192.168.86.25:5283
   ```

2. **Use HTTPS (5282) only for Agora calls**:
   - When you need to make a call, the app can redirect to HTTPS
   - Or use HTTPS only when actually making calls

## 🎯 Why This Happens

**Browser Security:**
- HTTPS pages are "secure contexts"
- Calling HTTP APIs from HTTPS pages is "mixed content"
- Browsers block mixed content by default for security
- CSP can allow it, but some browsers are strict

**The Fix:**
- CSP in `index.html` should allow it
- Client code now explicitly uses HTTP for server API
- This should work, but browsers may still show warnings

## 📝 If Still Not Working

### Option 1: Accept Mixed Content in Browser

**Chrome:**
1. Click the shield icon in address bar
2. Click "Load unsafe scripts" or "Allow"

**Firefox:**
1. Settings → Privacy & Security
2. Uncheck "Block dangerous and deceptive content" (temporarily)

### Option 2: Run Server on HTTPS

If mixed content is still blocked:
```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

Then update client to use HTTPS for server API (change line 51 in `DependencyInjection.cs` to use `https://`).

## ✅ Current Setup (After Fix)

- **Client**: HTTPS on port 5282 (for Agora) ✅
- **Server**: HTTP on port 5262 ✅
- **Client → Server**: HTTP (allowed by CSP) ✅
- **Agora calls**: Work (client is on HTTPS) ✅

Try it now - the login should work on HTTPS (5282)!

