# Install SSL Certificate in iOS Simulator

## The Problem

Even with `NSAllowsArbitraryLoads: true`, iOS Simulator **still validates SSL certificates**. You need to install and trust the certificate at the system level.

## ✅ Solution: Install Certificate in Simulator

### Step 1: Certificate is Ready

The certificate has been exported to: `/tmp/digitalocean-cert.cer`

### Step 2: Install Certificate in Simulator

**Method 1: Drag and Drop (Easiest)**

1. **Open Finder** on your Mac
2. **Navigate to**: `/tmp/digitalocean-cert.cer`
3. **Drag the file** into the iOS Simulator window
4. The certificate will open automatically

**Method 2: Via Safari**

1. **Open Safari in Simulator**
2. **Navigate to**: `https://159.65.242.79/api/health`
3. **Accept the certificate warning** (if shown)
4. The certificate should be accessible

### Step 3: Trust the Certificate

1. **Open Settings** in Simulator
2. **Go to**: **General → VPN & Device Management** (or **General → About → Certificate Trust Settings**)
3. **Find the certificate** (should show "159.65.242.79" or similar)
4. **Tap the certificate**
5. **Enable "Trust" toggle** or **"Full Trust for Root Certificates"**

### Step 4: Rebuild the App

After installing the certificate, rebuild the app:

```bash
cd MentalHealthMobileClean
npx expo run:ios
```

### Step 5: Try Login Again

The app should now be able to connect! ✅

## 🔍 Verify Certificate is Installed

1. Open **Settings** in Simulator
2. Go to: **General → About → Certificate Trust Settings**
3. You should see the certificate listed
4. Make sure it's **enabled/trusted**

## 🐛 If Still Not Working

### Option 1: Check Certificate Trust Settings

1. **Settings → General → About → Certificate Trust Settings**
2. Find the certificate
3. **Enable "Full Trust for Root Certificates"**

### Option 2: Reset and Reinstall

```bash
# Reset simulator
xcrun simctl erase all

# Rebuild app
cd MentalHealthMobileClean
npx expo run:ios

# Then:
# 1. Drag certificate into simulator again
# 2. Trust it in Settings
# 3. Try login
```

### Option 3: Use HTTP Temporarily (Testing Only)

For quick testing, you can temporarily use HTTP:

1. Edit `src/config/app.config.js`:
   ```javascript
   USE_HTTPS: false,
   SERVER_PORT: 80,
   ```

2. Rebuild: `npx expo run:ios`

**⚠️ Warning:** This is only for testing. Production must use HTTPS.

## ✅ Success Checklist

- [ ] Certificate file exists: `/tmp/digitalocean-cert.cer`
- [ ] Certificate dragged into Simulator
- [ ] Certificate appears in Settings → VPN & Device Management
- [ ] Certificate is trusted/enabled
- [ ] App rebuilt after certificate installation
- [ ] Login works successfully

## 💡 Why This is Needed

iOS has **strict SSL validation**:
- `NSAllowsArbitraryLoads: true` allows HTTP connections
- But **HTTPS still requires valid or trusted certificates**
- Self-signed certificates must be installed and trusted manually

Even though Safari can load the site, the app needs the certificate to be trusted at the system level.

