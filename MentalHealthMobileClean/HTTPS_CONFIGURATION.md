# HTTPS Configuration for MentalHealthMobileClean

## Overview
The server now uses HTTPS instead of HTTP. The mobile app has been updated to use HTTPS URLs.

## Changes Made

### 1. App.js
- Updated `getApiBaseUrl()` to use `https://` instead of `http://`
- Web: `https://localhost:5262/api`
- Mobile: `https://192.168.86.32:5262/api` (update IP as needed)

### 2. DocumentUploadService.js
- Updated `getApiBaseUrl()` to use `https://` instead of `http://`
- Same URLs as App.js

### 3. SignalRService.js
- No changes needed - it uses the `SIGNALR_HUB_URL` from App.js, which is now HTTPS

## Important Notes

### Self-Signed Certificates
The server uses a self-signed SSL certificate. Mobile apps may need special handling:

#### iOS
- iOS will reject self-signed certificates by default
- For development: You can add the certificate to your iOS device's trusted certificates
- For production: Use a valid SSL certificate (Let's Encrypt, etc.)

#### Android
- Android also rejects self-signed certificates by default
- For development: You may need to configure network security config
- For production: Use a valid SSL certificate

### Updating Server IP
If your server IP changes, update these files:
1. `App.js` - line 26: Update the IP address
2. `src/services/DocumentUploadService.js` - line 8: Update the IP address

### Production (DigitalOcean)
For production server, update the IP to your DigitalOcean droplet IP:
```javascript
return 'https://64.225.12.121/api'; // Your DigitalOcean IP
```

### Local Development
For local development on your Mac:
1. Make sure server is running on HTTPS: `https://localhost:5262`
2. Update IP to your Mac's local IP (e.g., `192.168.86.32`)
3. Accept the self-signed certificate in your browser first
4. For mobile: You may need to accept the certificate on the device

## Testing
1. Start the server with HTTPS enabled
2. Update the IP addresses in the mobile app
3. Test login and API calls
4. If you get SSL errors, you may need to:
   - Accept the certificate in browser first
   - Configure network security for Android
   - Add certificate to iOS trusted certificates

