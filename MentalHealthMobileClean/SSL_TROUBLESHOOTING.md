# SSL Certificate Troubleshooting

## Problem: "Network request failed" with HTTPS

If you're getting "Network request failed" errors when using HTTPS, it's likely due to the self-signed SSL certificate not being trusted by the device.

## Solutions

### Option 1: Use HTTP for Development (Quick Fix)

If you're just testing locally, you can temporarily use HTTP:

1. Edit `src/config/app.config.js`
2. Change `USE_HTTPS: true` to `USE_HTTPS: false`
3. Restart the app

**Note**: This is only for development. Production should always use HTTPS.

### Option 2: Install Certificate on Device (Recommended for Testing)

#### iOS (Physical Device)
1. On your Mac, export the certificate from Keychain Access
2. Email it to yourself or use AirDrop
3. Open the certificate file on your iPhone
4. Go to Settings > General > VPN & Device Management
5. Trust the certificate

#### iOS (Simulator)
1. Open Safari in the simulator
2. Navigate to `https://192.168.86.25:5262`
3. Accept the certificate warning
4. The certificate will be trusted for the simulator

#### Android
1. Download the certificate from your server
2. Install it on the device: Settings > Security > Install from storage
3. Or create a network security config (see below)

### Option 3: Verify Server is Running

Make sure your server is actually running and accessible:

```bash
# Test from terminal
curl -k https://192.168.86.25:5262/api/auth/login \
  -X POST \
  -H 'Content-Type: application/json' \
  -d '{"email":"test@test.com","password":"test"}'
```

If this fails, the server might not be running or accessible.

### Option 4: Check Firewall

Make sure port 5262 is open:

```bash
# macOS
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate
```

### Option 5: Use Production Server

If you have a production server with a valid SSL certificate, use that instead:

1. Edit `src/config/app.config.js`
2. Change `SERVER_IP` to your production server IP
3. Keep `USE_HTTPS: true`

## Current Configuration

Check your current config in `src/config/app.config.js`:
- `SERVER_IP`: Should match your server's IP
- `SERVER_PORT`: Should match your server's port (5262)
- `USE_HTTPS`: true for HTTPS, false for HTTP

## Debugging Steps

1. **Check logs**: Look for the API URL in console logs
   ```
   📱 Using Mobile API URL: https://192.168.86.25:5262/api
   ```

2. **Test server directly**: Use curl to verify server is accessible
   ```bash
   curl -k https://192.168.86.25:5262/api/auth/login
   ```

3. **Check network**: Make sure your device and Mac are on the same network

4. **Try HTTP first**: Temporarily set `USE_HTTPS: false` to verify network connectivity

5. **Check server logs**: Look at server logs to see if requests are reaching it

## Common Issues

### "Network request failed"
- Server not running
- Wrong IP address
- Firewall blocking connection
- SSL certificate not trusted

### "SSL certificate error"
- Self-signed certificate not installed on device
- Certificate expired
- Certificate doesn't match the IP/domain

### "Connection timeout"
- Server not accessible from device
- Wrong IP address
- Firewall blocking port 5262

## Quick Test

To quickly test if it's an SSL issue:

1. Change `USE_HTTPS: false` in config
2. Make sure server is running on HTTP (port 5262)
3. Try login again
4. If it works with HTTP, the issue is SSL certificate trust

