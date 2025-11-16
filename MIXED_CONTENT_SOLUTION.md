# Mixed Content Error Solution

## 🚨 The Problem

You're getting this error:
```
Mixed Content: The page at 'https://192.168.86.25:5282/...' was loaded over HTTPS, 
but requested an insecure resource 'http://192.168.86.25:5262/api/realtime/token...'
```

This happens because:
- **Client** runs on **HTTPS** (required for Agora SDK)
- **Server** runs on **HTTP** (port 5262)
- Browsers **block** HTTPS pages from calling HTTP APIs (mixed content security)

## ✅ Solution Options

### Option 1: Run Server on HTTPS (Recommended)

1. **Update `launchSettings.json`** in `SM_MentalHealthApp.Server/Properties/`:
   ```json
   {
     "profiles": {
       "http": {
         "applicationUrl": "http://localhost:5262"
       },
       "https": {
         "applicationUrl": "https://localhost:5262;http://localhost:5262"
       }
     }
   }
   ```

2. **Run server with HTTPS**:
   ```bash
   cd SM_MentalHealthApp.Server
   dotnet run --launch-profile https
   ```

3. **Update client `DependencyInjection.cs`** to use HTTPS:
   ```csharp
   var serverUrl = $"https://{baseUri.Host}:5262/";
   ```

### Option 2: Use HTTP for Client (Not Recommended)

**⚠️ This will break Agora calls** because Agora SDK requires HTTPS or localhost.

### Option 3: Browser Workaround (Development Only)

Some browsers allow you to disable mixed content blocking:
- **Chrome**: Click the shield icon in address bar → "Load unsafe scripts"
- **Firefox**: Settings → Privacy & Security → Uncheck "Block dangerous and deceptive content"

**⚠️ This is NOT a production solution!**

## 🎯 Recommended Approach

**Run both client and server on HTTPS:**

1. **Server**: Configure HTTPS in `launchSettings.json`
2. **Client**: Already configured for HTTPS (port 5282)
3. **Update HttpClient**: Change to use `https://` for server API

This ensures:
- ✅ Agora SDK works (requires HTTPS)
- ✅ No mixed content errors
- ✅ Secure communication
- ✅ Production-ready

## 📝 Quick Fix

The easiest fix is to update the server's `launchSettings.json` to include an HTTPS profile and run the server with HTTPS, then update the client's HttpClient configuration to use HTTPS for the server API.

