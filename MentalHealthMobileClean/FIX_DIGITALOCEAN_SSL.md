# Fix: "unable to get to https://ip/api" Error

## The Problem

You're getting "unable to get to https://ip/api" when trying to login. This is an **SSL certificate trust issue** in the iOS Simulator.

The DigitalOcean server uses HTTPS with a self-signed certificate, and the simulator needs to trust it.

## ✅ Solution: Trust the Certificate in Simulator

### Step 1: Open Safari in Simulator

1. **Open iOS Simulator** (if not already open)
2. **Open Safari** app in the simulator
3. **Navigate to**: `https://159.65.242.79/api/health`

### Step 2: Accept the Certificate Warning

1. Safari will show a security warning page
2. Click **"Show Details"** or **"Advanced"**
3. Click **"Visit Website"** or **"Proceed to 159.65.242.79 (unsafe)"**
4. The certificate will now be trusted for the simulator

### Step 3: Verify It Worked

1. The page should load (you'll see "healthy" or similar JSON response)
2. You may see a warning icon in the address bar (this is normal for self-signed certs)
3. Close Safari

### Step 4: Try Login Again

1. Go back to your React Native app
2. Try logging in again
3. It should work now! ✅

## 🔍 Why This Happens

iOS Simulator requires you to manually trust self-signed certificates, even with `NSAllowsArbitraryLoads: true` in the app configuration. The simulator shares the certificate trust store with Safari.

## 🐛 If Still Not Working

### Option 1: Check the URL

Make sure the app is using the correct URL. Check Metro console logs - you should see:
```
API URL: https://159.65.242.79/api/auth/login
```

If you see `https://ip/api` instead, the config isn't being read correctly.

### Option 2: Verify Config is Loaded

Add a console log in `App.js` to verify the config:

```javascript
import AppConfig from './config/app.config';
console.log('API Base URL:', AppConfig.API_BASE_URL);
```

Should show: `https://159.65.242.79/api`

### Option 3: Test from Terminal

Verify the server is accessible:

```bash
curl -k https://159.65.242.79/api/health
```

Should return: `"healthy"` or similar

### Option 4: Reset Simulator and Try Again

```bash
xcrun simctl erase all
```

Then:
1. Rebuild app: `npx expo run:ios`
2. Trust certificate in Safari again
3. Try login

## ✅ Success Checklist

- [ ] Safari in Simulator can load `https://159.65.242.79/api/health`
- [ ] Certificate warning was accepted in Safari
- [ ] App shows correct API URL in console logs (not "ip")
- [ ] Login works successfully

## 💡 Pro Tip

**The certificate trust is per simulator instance.** If you:
- Reset the simulator
- Create a new simulator
- Erase simulator content

You'll need to trust the certificate again in Safari.

