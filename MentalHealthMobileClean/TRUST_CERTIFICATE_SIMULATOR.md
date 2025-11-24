# Trust Certificate in iOS Simulator - Step by Step

## Quick Fix for SSL Error in Simulator

If you're getting SSL errors when accessing `https://192.168.86.25:5262/swagger/index.html` in the simulator, you need to trust the certificate.

## Method 1: Trust via Safari (Easiest)

### Step-by-Step:

1. **Open Safari in iOS Simulator**
   - Make sure the simulator is running
   - Click Safari icon in the simulator

2. **Navigate to the server**
   - In Safari's address bar, type: `https://192.168.86.25:5262/swagger/index.html`
   - Press Enter

3. **Accept the certificate warning**
   - Safari will show a security warning page
   - Look for a button like "Show Details", "Advanced", or "Details"
   - Click it
   - You'll see an option like "Visit Website" or "Proceed to 192.168.86.25 (unsafe)"
   - Click that button

4. **Verify it worked**
   - The page should now load (you might see Swagger UI or an API response)
   - You may see a warning icon in the address bar (this is normal for self-signed certs)

5. **Test your app**
   - Close Safari
   - Go back to your React Native app
   - Try logging in - it should work now!

## Method 2: Install Certificate File

If Method 1 doesn't work:

1. **Export the certificate**:
   ```bash
   openssl s_client -showcerts -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -outform DER > /tmp/server-5262.cer
   ```

2. **Transfer to Simulator**:
   - Drag the `.cer` file from `/tmp/server-5262.cer` into the Simulator window
   - Or use Safari in Simulator to download it

3. **Install in Simulator**:
   - The certificate should open automatically
   - Or go to: Settings > General > VPN & Device Management
   - Find the certificate and tap it
   - Enable the "Trust" toggle

## Troubleshooting

### Still getting SSL errors?

1. **Clear Safari cache**:
   - Safari > Settings > Clear History and Website Data
   - Try again

2. **Reset Simulator**:
   - Device > Erase All Content and Settings
   - This will remove all trusted certificates
   - You'll need to trust the certificate again

3. **Check certificate matches**:
   ```bash
   # Verify certificate details
   openssl s_client -connect 192.168.86.25:5262 </dev/null 2>/dev/null | openssl x509 -noout -subject -issuer
   ```

4. **Verify server is running**:
   ```bash
   curl -k https://192.168.86.25:5262/swagger/index.html
   ```

### Certificate not persisting?

- Certificates trusted in Safari should persist across app restarts
- If they don't, you may need to re-trust after simulator restarts
- This is a known iOS Simulator behavior

## Important Notes

- ⚠️ **Each port has its own certificate**: Port 5262 and 5282 have different certificates
- ✅ **Trust is per-port**: You need to trust the certificate for port 5262 specifically
- ✅ **Safari trust = App trust**: Once trusted in Safari, all apps can use it
- ⚠️ **Simulator resets**: Erasing simulator will remove trusted certificates

## Quick Test

After trusting, test with:
```bash
# In Safari simulator, navigate to:
https://192.168.86.25:5262/swagger/index.html
```

Should load without errors!

