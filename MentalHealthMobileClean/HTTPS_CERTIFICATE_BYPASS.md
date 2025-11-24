# HTTPS Certificate Bypass for Development

## Overview

This app requires HTTPS because **Agora (video/audio calling) mandates HTTPS**. However, for local development, the server uses a **self-signed SSL certificate**, which mobile devices reject by default.

This document explains how we've configured the app to bypass certificate validation for development purposes.

## ⚠️ Important Security Note

**This configuration is for DEVELOPMENT ONLY!**

- Self-signed certificates are **NOT secure** for production
- Certificate bypass should **NEVER** be enabled in production builds
- Always use valid SSL certificates (Let's Encrypt, etc.) in production

## 🔧 How It Works

The certificate bypass happens at the **native platform level**, not in JavaScript:

### Android
- **File**: `android/app/src/main/res/xml/network_security_config.xml`
- **Configuration**: Allows user-installed certificates (self-signed certs)
- **Manifest**: `AndroidManifest.xml` references the network security config

### iOS
- **File**: `app.json` (Expo configuration)
- **Configuration**: App Transport Security (ATS) settings
- **Settings**: `NSAllowsArbitraryLoads` and domain exceptions

## 📋 Configuration Details

### Android Network Security Config

Located at: `android/app/src/main/res/xml/network_security_config.xml`

```xml
<network-security-config>
    <base-config cleartextTrafficPermitted="false">
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />  <!-- Allows self-signed certs -->
        </trust-anchors>
    </base-config>
    
    <domain-config cleartextTrafficPermitted="false">
        <domain includeSubdomains="true">192.168.86.25</domain>
        <domain includeSubdomains="true">localhost</domain>
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />
        </trust-anchors>
    </domain-config>
</network-security-config>
```

**Key Points:**
- `cleartextTrafficPermitted="false"` - Forces HTTPS (no HTTP)
- `<certificates src="user" />` - Allows self-signed certificates
- Domain-specific config for your server IP

### iOS App Transport Security

Located in: `app.json`

```json
{
  "ios": {
    "infoPlist": {
      "NSAppTransportSecurity": {
        "NSAllowsArbitraryLoads": true,
        "NSExceptionDomains": {
          "192.168.86.25": {
            "NSExceptionAllowsInsecureHTTPLoads": false,
            "NSExceptionRequiresForwardSecrecy": false,
            "NSIncludesSubdomains": true
          }
        }
      }
    }
  }
}
```

**Key Points:**
- `NSAllowsArbitraryLoads: true` - Allows connections to any domain (dev only!)
- Domain exceptions for your server IP
- Still requires HTTPS (`NSExceptionAllowsInsecureHTTPLoads: false`)

## 🚀 Usage

### Standard Fetch (Recommended)

The app uses standard `fetch()` API. The certificate bypass happens automatically at the native level:

```javascript
// Standard fetch - works with certificate bypass
const response = await fetch('https://192.168.86.25:5262/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});
```

### Enhanced Fetch Utility (Optional)

We've also created a utility wrapper with better error messages:

```javascript
import { fetchWithCertBypass, fetchJson } from './src/utils/fetchWithCertBypass';

// Enhanced fetch with better SSL error messages
const response = await fetchWithCertBypass(url, options);

// Fetch and parse JSON in one call
const data = await fetchJson(url, options);
```

## 🔄 Updating Server IP

If your server IP changes, update it in **two places**:

1. **Network Security Config** (Android):
   - Edit `android/app/src/main/res/xml/network_security_config.xml`
   - Update the `<domain>` entries

2. **App Transport Security** (iOS):
   - Edit `app.json`
   - Update the `NSExceptionDomains` entries

3. **App Config** (JavaScript):
   - Edit `src/config/app.config.js`
   - Update `SERVER_IP`

## 🧪 Testing

### Verify Certificate Bypass is Working

1. **Start the server** on HTTPS:
   ```bash
   dotnet run --launch-profile https
   ```

2. **Check server is accessible**:
   - Browser: `https://192.168.86.25:5262/swagger/index.html`
   - Accept the self-signed certificate warning in browser

3. **Test from mobile app**:
   - Login should work without SSL errors
   - API calls should succeed
   - Check console logs for any SSL warnings

### Common Issues

#### Android: "Network request failed"
- **Solution**: Rebuild the app after changing `network_security_config.xml`
  ```bash
   npx expo run:android
   ```

#### iOS: "App Transport Security has blocked a cleartext HTTP"
- **Solution**: Make sure `NSAllowsArbitraryLoads` is `true` in `app.json`
- Rebuild: `npx expo run:ios`

#### Both: Certificate errors persist
- **Check**: Server is actually running on HTTPS (not HTTP)
- **Check**: Server IP matches configuration
- **Check**: App was rebuilt after config changes

## 📝 Production Checklist

Before deploying to production:

- [ ] Remove or restrict `NSAllowsArbitraryLoads` in iOS config
- [ ] Remove `<certificates src="user" />` from Android config (or restrict to specific domains)
- [ ] Use a valid SSL certificate (Let's Encrypt, commercial CA, etc.)
- [ ] Test with production certificate before deploying
- [ ] Update server IP to production domain/IP
- [ ] Remove development-specific domain exceptions

## 🔗 Related Files

- `src/config/app.config.js` - Server configuration
- `android/app/src/main/res/xml/network_security_config.xml` - Android cert bypass
- `android/app/src/main/AndroidManifest.xml` - References network config
- `app.json` - iOS ATS configuration
- `src/utils/fetchWithCertBypass.js` - Enhanced fetch utility

## 📚 Additional Resources

- [Android Network Security Config](https://developer.android.com/training/articles/security-config)
- [iOS App Transport Security](https://developer.apple.com/documentation/security/preventing_insecure_network_connections)
- [Agora HTTPS Requirements](https://docs.agora.io/en/video-calling/get-started/get-started-sdk?platform=react-native)

