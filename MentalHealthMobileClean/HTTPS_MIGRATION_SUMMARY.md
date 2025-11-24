# HTTPS Migration Summary for MentalHealthMobileClean

## ✅ Changes Made

### 1. **App.js** (Main Application File)
- **Line 25**: Changed `http://localhost:5262/api` → `https://localhost:5262/api`
- **Line 26**: Changed `http://192.168.86.32:5262/api` → `https://192.168.86.32:5262/api`
- **Impact**: All API calls now use HTTPS
- **SignalR**: Automatically uses HTTPS (derived from `API_BASE_URL`)

### 2. **src/services/DocumentUploadService.js**
- **Line 7**: Changed `http://localhost:5262/api` → `https://localhost:5262/api`
- **Line 8**: Changed `http://192.168.86.27:5262/api` → `https://192.168.86.32:5262/api`
- **Impact**: Document upload service now uses HTTPS

### 3. **update-network-ip.sh** (IP Update Script)
- Updated to replace HTTPS URLs (not just HTTP)
- Now updates both `App.js` and `DocumentUploadService.js`
- Updated curl command to use `-k` flag for self-signed certificates
- Updated server command to show HTTPS only

## 📋 Files That Use HTTPS (Automatically Updated)

1. ✅ **App.js** - All fetch calls use `API_BASE_URL` (now HTTPS)
2. ✅ **DocumentUploadService.js** - All API calls use HTTPS
3. ✅ **SignalRService.js** - Uses `SIGNALR_HUB_URL` from App.js (now HTTPS)
4. ✅ **SmsComponent.js** - Uses `apiBaseUrl` prop from App.js (now HTTPS)

## ⚠️ Important Considerations

### Self-Signed Certificates
The server uses a self-signed SSL certificate. This may cause issues on mobile devices:

#### iOS
- iOS will reject self-signed certificates by default
- **Solution for Development**: 
  - Access the server URL in Safari on your Mac first
  - Accept the certificate warning
  - For iOS device: May need to install certificate profile

#### Android
- Android also rejects self-signed certificates
- **Solution for Development**:
  - Create `android/app/src/main/res/xml/network_security_config.xml`:
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <network-security-config>
        <domain-config cleartextTrafficPermitted="false">
            <domain includeSubdomains="true">192.168.86.32</domain>
            <trust-anchors>
                <certificates src="system" />
                <certificates src="user" />
            </trust-anchors>
        </domain-config>
    </network-security-config>
    ```
  - Update `AndroidManifest.xml` to reference it

### Production Server (DigitalOcean)
For production, update the IP address to your DigitalOcean server:
```javascript
// In App.js and DocumentUploadService.js
return 'https://64.225.12.121/api'; // Your DigitalOcean IP
```

## 🔍 Testing Checklist

- [ ] Update IP address in `App.js` (line 26) to match your server
- [ ] Update IP address in `DocumentUploadService.js` (line 8) to match your server
- [ ] Test login functionality
- [ ] Test API calls (messages, calls, etc.)
- [ ] Test SignalR connection
- [ ] Test document upload
- [ ] Test emergency button
- [ ] Test SMS functionality
- [ ] If SSL errors occur, configure certificate trust (see above)

## 🚀 Next Steps

1. **Update IP Addresses**: Change `192.168.86.32` to your actual server IP
2. **Test Connection**: Try logging in from the mobile app
3. **Handle SSL Errors**: If you get SSL certificate errors, follow the platform-specific solutions above
4. **For Production**: Consider using a valid SSL certificate (Let's Encrypt) instead of self-signed

## 📝 Notes

- All HTTP URLs have been changed to HTTPS
- SignalR automatically uses HTTPS since it derives from `API_BASE_URL`
- The emergency endpoint (`/api/emergency/test-emergency`) now uses HTTPS
- All other API endpoints now use HTTPS

