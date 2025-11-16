# Setup: Run Server on HTTPS to Fix Mixed Content

## 🎯 The Problem

- Client on HTTPS (5282) ✅ (needed for Agora)
- Server on HTTP (5262) ✅ (works)
- Browser blocks HTTPS → HTTP calls ❌ (mixed content)

## ✅ Solution: Run Server on HTTPS

### Step 1: Start Server with HTTPS

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

Server will run on:
- **HTTPS**: `https://0.0.0.0:5262`
- **HTTP**: `http://0.0.0.0:5262` (also available)

### Step 2: Update Client to Use HTTPS for Server API

I'll update the client code to use HTTPS when server is on HTTPS.

### Step 3: Accept Certificate Warning

When you first access `https://192.168.86.25:5262`, accept the self-signed certificate warning.

## 🔄 Alternative: Keep HTTP Server + Allow Mixed Content

If you prefer to keep server on HTTP:

1. **Access client via HTTPS (5282)**
2. **When browser shows mixed content warning, click "Allow"**
3. **Login should work**

This is simpler for development, but you'll need to allow mixed content each time.

## 📝 Current Status

- ✅ Client code updated to always use HTTP for server API
- ✅ CSP allows mixed content
- ⚠️ Browser may still block (depends on browser settings)

**Try Option 1 first** (allow mixed content in browser) - it's the quickest fix!

