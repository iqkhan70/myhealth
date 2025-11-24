# iOS Certificate Fix - Trust Self-Signed Certificate in Simulator

## The Problem

iOS **does not allow bypassing certificate validation** through App Transport Security settings alone. Even with `NSAllowsArbitraryLoads: true`, iOS will still reject self-signed certificates for HTTPS connections.

## Solution: Trust the Certificate in Simulator

You need to manually trust the certificate in the iOS Simulator.

### Step 1: Open Safari in Simulator

1. Open iOS Simulator
2. Open Safari app in the simulator
3. Navigate to: `https://192.168.86.25:5262/swagger/index.html`

### Step 2: Accept the Certificate

1. Safari will show a security warning
2. Click "Show Details" or "Advanced"
3. Click "Visit Website" or "Proceed to 192.168.86.25 (unsafe)"
4. The certificate will now be trusted for the simulator

### Step 3: Verify

1. The page should load (you might see Swagger UI or an API response)
2. Close Safari
3. Go back to your React Native app
4. Try logging in again - it should work now!

## Alternative: Install Certificate on Physical Device

If you're using a physical iOS device:

1. **Export the certificate** from your Mac:
   ```bash
   # Get the certificate from the server
   openssl s_client -showcerts -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -outform DER > server-cert.cer
   ```

2. **Transfer to device**: Email it to yourself or use AirDrop

3. **Install on device**:
   - Open the .cer file on your iPhone
   - Go to Settings > General > VPN & Device Management
   - Tap on the certificate
   - Tap "Install" and trust it

## Why This Happens

iOS has strict security requirements:
- `NSAllowsArbitraryLoads` only allows HTTP (not HTTPS with invalid certs)
- iOS requires valid certificates OR user-trusted certificates
- The simulator shares the certificate trust store with Safari

## After Trusting the Certificate

Once you've trusted the certificate in Safari (simulator) or installed it on your device:
- ✅ Your React Native app will be able to connect
- ✅ All HTTPS requests will work
- ✅ No more "Network request failed" errors

## Quick Test

After trusting the certificate, test with:

```bash
# In Safari simulator, navigate to:
https://192.168.86.25:5262/swagger/index.html

# Should load without errors
```

Then try logging in from your app - it should work!

