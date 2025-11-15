# Fixing Mixed Content Warning (HTTPS → HTTP)

## 🚨 The Problem

When accessing the client over HTTPS (`https://macip:5282`), the browser shows:
```
The page at 'https://macip:5282' was loaded over HTTPS, but requested an insecure resource 'http://macip:5262/api/...'
```

This happens because:
- **Client runs on HTTPS** (required for Agora SDK)
- **Server API runs on HTTP** (no need for HTTPS)
- **Browser blocks mixed content** (HTTP requests from HTTPS pages)

## ✅ Solution Applied

Added a Content Security Policy meta tag to `index.html` that allows mixed content for API calls:

```html
<meta http-equiv="Content-Security-Policy" content="upgrade-insecure-requests; connect-src 'self' http://*:* https://*:* ws://*:* wss://*:*;">
```

This tells the browser:
- `upgrade-insecure-requests`: Try to upgrade HTTP to HTTPS when possible
- `connect-src`: Allow connections to HTTP and HTTPS endpoints

## 🔍 Alternative Solutions

If the meta tag doesn't work in all browsers, you have these options:

### Option 1: Browser Settings (Development Only)
Some browsers allow you to disable mixed content blocking:
- **Chrome**: Settings → Privacy → Site Settings → Insecure content → Allow
- **Firefox**: `about:config` → `security.mixed_content.block_active_content` → `false`

### Option 2: Use a Proxy/Reverse Proxy
Set up nginx or similar to proxy HTTP API calls through HTTPS:
```
HTTPS Client → HTTPS Proxy → HTTP Server API
```

### Option 3: Run Server on HTTPS (Not Recommended)
You could run the server on HTTPS, but this adds complexity and isn't necessary since the server doesn't access media devices.

## 📝 Why This Setup?

- **Client (HTTPS)**: Required for Agora SDK (microphone/camera access)
- **Server (HTTP)**: Just serves API endpoints, doesn't need HTTPS
- **Mixed Content**: Browsers allow this for API calls (fetch/XHR), but may show warnings

## ✅ Testing

After applying the fix:

1. **Restart the client:**
   ```bash
   cd SM_MentalHealthApp.Client
   dotnet run --launch-profile https
   ```

2. **Access via HTTPS:**
   - `https://macip:5282`

3. **Try making a call:**
   - The mixed content warning should be gone or suppressed
   - API calls should work normally

## 🎯 Expected Behavior

- ✅ **API calls work**: Client (HTTPS) can call Server (HTTP)
- ✅ **Agora works**: Client is HTTPS, so Agora SDK works
- ⚠️ **Browser warnings**: May still show in console, but won't block functionality
- ✅ **Calls work**: Both audio and video calls should function normally

## 📚 References

- [MDN: Mixed Content](https://developer.mozilla.org/en-US/docs/Web/Security/Mixed_content)
- [Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

