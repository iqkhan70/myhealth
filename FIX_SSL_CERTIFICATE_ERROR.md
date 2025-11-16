# Fix "Failed to Fetch" / SSL Certificate Error

## Problem
When accessing from another machine via IP address (e.g., `https://192.168.86.25:5282`), you get:
```
TypeError: failed to fetch
```

## Root Cause
The browser is blocking the request because the self-signed SSL certificate is not trusted.

## Solution 1: Accept Certificate in Browser (Quick Fix)

1. **First, access the server directly in the browser:**
   ```
   https://192.168.86.25:5262/api/health
   ```

2. **You'll see a security warning** - click "Advanced" or "Show Details"

3. **Click "Proceed to 192.168.86.25 (unsafe)"** or "Accept the Risk and Continue"

4. **Now try accessing the client again:**
   ```
   https://192.168.86.25:5282
   ```

The browser will now trust the certificate for this session.

## Solution 2: Trust Certificate on Client Machine (Permanent)

### On macOS:
```bash
# Get the certificate from the server
openssl s_client -connect 192.168.86.25:5262 -showcerts < /dev/null 2>/dev/null | openssl x509 -outform PEM > server-cert.pem

# Add to keychain (you'll be prompted for password)
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain server-cert.pem
```

### On Windows:
1. Access `https://192.168.86.25:5262/api/health` in browser
2. Click the lock icon in address bar
3. Click "Certificate"
4. Go to "Details" tab
5. Click "Copy to File"
6. Export as Base-64 encoded X.509 (.CER)
7. Double-click the .CER file
8. Click "Install Certificate"
9. Choose "Local Machine" → "Place all certificates in the following store" → "Trusted Root Certification Authorities"

## Solution 3: Use HTTP Instead of HTTPS (Development Only)

**⚠️ WARNING: Only for development on local network!**

If you're on the same local network and don't need HTTPS, you can temporarily use HTTP:

1. **Update server to also listen on HTTP:**
   - Already configured in `launchSettings.json` (port 5283 for client, but server should also have HTTP)

2. **Update client HttpClient to use HTTP for IP addresses:**
   - Currently it uses HTTPS for IP addresses
   - You can temporarily change this in `DependencyInjection.cs`

3. **Access via HTTP:**
   ```
   http://192.168.86.25:5282
   ```

**Note:** This won't work with Agora (requires HTTPS), so only use if you're not testing calls.

## Solution 4: Use Ngrok (Recommended for Testing)

Ngrok provides valid SSL certificates automatically:

1. Start server ngrok: `./start-ngrok-server.sh`
2. Start client ngrok: `./start-ngrok-client.sh`
3. Access: `https://client-ngrok-url.ngrok.io?server=https://server-ngrok-url.ngrok.io`

## Check What's Happening

Open browser console (F12) and look for:
- Network errors
- CORS errors
- SSL/TLS errors
- The actual URL being called

The console will show the exact error message.

## Quick Test

Test if server is accessible:
```bash
# From the other machine
curl -k https://192.168.86.25:5262/api/health
```

If this works, the issue is browser certificate validation.
If this fails, check firewall/network connectivity.

