# Configuration Guide

## 📁 Config File Location

All server configuration is centralized in:

```
src/config/app.config.js
```

## 🔧 Configuration Options

### Server IP Address

```javascript
SERVER_IP: "192.168.86.25";
```

Update this when your Mac's IP address changes.

### Server Port

```javascript
SERVER_PORT: 5262;
```

The port your server API is running on. Mobile app connects directly to the server (not through the Blazor client on 5282).

### HTTPS/HTTP

```javascript
USE_HTTPS: true;
```

Set to `false` if using HTTP (not recommended for production).

## 🔄 How to Update IP Address

### Option 1: Edit Config File Directly

1. Open `src/config/app.config.js`
2. Change `SERVER_IP: '192.168.86.25'` to your new IP
3. Save the file
4. Restart the app

### Option 2: Use Update Script (Recommended)

```bash
# Auto-detect IP
./update-network-ip.sh

# Or specify IP manually
./update-network-ip.sh 192.168.86.25
```

## 📱 Where Config is Used

The config file is imported and used in:

- `App.js` - Main app API calls
- `src/services/DocumentUploadService.js` - Document upload service

## ✅ Benefits

- **Single source of truth** - Change IP in one place
- **Easy updates** - No need to search through code
- **Less errors** - No risk of missing an IP address
- **Script support** - Automated IP updates

## 🚀 Quick Start

1. **First time setup**: Edit `src/config/app.config.js` with your server IP
2. **IP changed?**: Run `./update-network-ip.sh` or edit the config file
3. **That's it!** All API calls will use the new IP automatically

## 🔒 HTTPS & Certificate Bypass

**Important**: This app uses HTTPS because Agora requires it. For local development with self-signed certificates, see:

- `HTTPS_CERTIFICATE_BYPASS.md` - Complete guide to certificate bypass setup
- Certificate bypass is configured at the native level (Android/iOS)
- The app automatically handles self-signed certificates in development
