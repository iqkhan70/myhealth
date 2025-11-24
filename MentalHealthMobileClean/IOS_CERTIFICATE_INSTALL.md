# iOS Certificate Installation - Complete Guide

## The Problem

iOS **strictly validates SSL certificates** and will reject:
- Self-signed certificates
- Certificates with wrong Common Name (CN)
- Certificates that don't match the hostname/IP

Your server certificate is issued to `CN=localhost` but you're connecting via IP `192.168.86.25`, so iOS rejects it.

## Solution: Install and Trust the Certificate

### For iOS Simulator

#### Method 1: Trust in Safari (Easiest)

1. **Open Safari in Simulator**
   - Launch iOS Simulator
   - Open Safari app

2. **Navigate to your server**
   - Go to: `https://192.168.86.25:5262/swagger/index.html` (server API)
   - Safari will show a security warning

3. **Accept the certificate**
   - Click "Show Details" or "Advanced"
   - Click "Visit Website" or "Proceed to 192.168.86.25 (unsafe)"
   - The certificate is now trusted

4. **Verify**
   - The page should load
   - Close Safari
   - Test your React Native app - it should work now!

#### Method 2: Install Certificate File

1. **Export certificate** from server (port 5262):
   ```bash
   openssl s_client -showcerts -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -outform DER > server-cert.cer
   ```

2. **Transfer to Simulator**:
   - Drag the `.cer` file into the Simulator
   - Or use Safari in Simulator to download it

3. **Install**:
   - Tap the certificate file in Simulator
   - Go to Settings > General > VPN & Device Management
   - Tap the certificate
   - Enable "Trust" toggle

### For Physical iPhone/iPad

#### Method 1: Install via Email/AirDrop

1. **Export certificate** from server (port 5262):
   ```bash
   openssl s_client -showcerts -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -outform DER > server-cert.cer
   ```

2. **Transfer to device**:
   - Email the `.cer` file to yourself
   - Or use AirDrop to send it to your iPhone

3. **Install on device**:
   - Open the email/AirDrop on your iPhone
   - Tap the `.cer` file
   - Go to Settings > General > VPN & Device Management
   - Tap the certificate under "Downloaded Profile"
   - Tap "Install" (enter passcode if prompted)
   - Go back to Settings > General > About > Certificate Trust Settings
   - Enable "Full Trust" for the certificate

#### Method 2: Trust via Safari

1. **On your iPhone**, open Safari
2. Navigate to: `https://192.168.86.25:5262/swagger/index.html`
3. Accept the security warning
4. The certificate will be trusted

## Alternative: Generate Certificate with IP in CN

If you want a certificate that matches your IP address:

1. **Generate new certificate with IP**:
   ```bash
   # Create certificate with IP in Subject Alternative Name (SAN)
   # This requires modifying your server's certificate generation
   ```

2. **Update server to use new certificate**

This is more complex but provides a better certificate match.

## Quick Test

After installing the certificate:

1. **Test in Safari**:
   - Navigate to `https://192.168.86.25:5262/swagger/index.html`
   - Should load without errors

2. **Test in your app**:
   - Try logging in
   - Should work without "Network request failed" error

## Troubleshooting

### Certificate still not trusted

1. **Check certificate is installed**:
   - Settings > General > VPN & Device Management
   - Should see your certificate listed

2. **Enable full trust** (iOS 10.3+):
   - Settings > General > About > Certificate Trust Settings
   - Enable "Full Trust" for your certificate

3. **Restart app**:
   - Force quit the app
   - Reopen it

### Still getting SSL errors

1. **Verify certificate matches**:
   ```bash
   openssl s_client -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -noout -subject -issuer
   ```

2. **Check server is running**:
   ```bash
   curl -k https://192.168.86.25:5262/swagger/index.html
   ```

3. **Clear app cache**:
   - Delete and reinstall the app
   - Or: Settings > General > iPhone Storage > Your App > Offload App

## Important Notes

- ⚠️ **Development Only**: Self-signed certificates are NOT secure for production
- ⚠️ **Certificate Expiry**: Self-signed certs expire (check date with openssl command)
- ✅ **After Trusting**: All apps on the device can use the certificate
- ✅ **Persistent**: Certificate trust persists until you remove it

## Certificate Location

The certificate has been exported to: `/tmp/server-cert.cer`

You can:
- Email it to yourself
- AirDrop it to your iPhone
- Drag it into the Simulator
- Share it via any method

