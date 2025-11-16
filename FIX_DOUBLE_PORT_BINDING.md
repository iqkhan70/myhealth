# Fix: Port 5262 Binding Twice

## 🚨 The Problem

The HTTPS profile was trying to bind **both HTTP and HTTPS to the same port 5262**:

```json
"applicationUrl": "https://0.0.0.0:5262;http://0.0.0.0:5262"
```

**You can't bind both protocols to the same port!** This causes the "address already in use" error.

## ✅ Fixed

I've updated the HTTPS profile to **only use HTTPS on port 5262**:

```json
"applicationUrl": "https://0.0.0.0:5262"
```

## 🚀 Now Start Server

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

**Server will run on:**
- ✅ **HTTPS**: `https://0.0.0.0:5262` (accessible via `https://192.168.86.25:5262`)

## 📝 If You Need Both HTTP and HTTPS

If you want both protocols available, use **different ports**:

```json
"applicationUrl": "https://0.0.0.0:5262;http://0.0.0.0:5263"
```

But since you're using HTTPS for everything, you only need HTTPS on 5262.

## ✅ Verify

After starting, you should see:
```
Now listening on: https://0.0.0.0:5262
Application started. Press Ctrl+C to shut down.
```

No more "address already in use" error!

