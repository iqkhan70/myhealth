# Setup: Both Client and Server on HTTPS

## ✅ Configuration Complete

Both client and server are now configured to use HTTPS.

## 🚀 How to Run

### Terminal 1: Start Server (HTTPS)

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

**Server will run on:**
- **HTTPS**: `https://0.0.0.0:5262` (accessible via `https://192.168.86.25:5262`)
- **HTTP**: `http://0.0.0.0:5262` (also available, but use HTTPS)

### Terminal 2: Start Client (HTTPS)

```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```

**Client will run on:**
- **HTTPS**: `https://0.0.0.0:5282` (accessible via `https://192.168.86.25:5282`)
- **HTTP**: `http://0.0.0.0:5283` (also available, but use HTTPS)

## 🌐 Access from Other Machine

```
https://192.168.86.25:5282
```

**First time:** Accept the self-signed certificate warning (safe for development)

## ✅ What Changed

1. **Client**: Updated to use HTTPS for server API when accessing via IP address
2. **Server**: Already configured for HTTPS in `launchSettings.json`
3. **No mixed content**: Both use HTTPS, so no browser blocking

## 🔒 Certificate Warnings

You'll see certificate warnings because we're using self-signed certificates. This is normal for development.

**To accept:**
- Chrome/Edge: Click "Advanced" → "Proceed to 192.168.86.25 (unsafe)"
- Firefox: Click "Advanced" → "Accept the Risk and Continue"

## 🎯 Benefits

- ✅ No mixed content issues
- ✅ Agora SDK works (requires HTTPS)
- ✅ Production-ready setup
- ✅ Secure communication
- ✅ Can implement proper certificates later

## 📝 Next Steps (For Production)

When ready for production:
1. Get proper SSL certificates (Let's Encrypt, etc.)
2. Configure server to use the certificates
3. Update `launchSettings.json` to use production certificates
4. No more certificate warnings!

## 🧪 Testing

1. **Start server**: `dotnet run --launch-profile https` (in Server directory)
2. **Start client**: `dotnet run --launch-profile https` (in Client directory)
3. **Access**: `https://192.168.86.25:5282`
4. **Accept certificate**: Click through the warning
5. **Login**: Should work now! ✅

