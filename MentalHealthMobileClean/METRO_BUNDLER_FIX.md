# Metro Bundler "unsanitizedScriptURLString" Fix

## The Error

`unsanitizedScriptURLString = (null)` means the app can't load the JavaScript bundle from Metro bundler.

## Quick Fixes

### Solution 1: Restart Metro Bundler

```bash
cd MentalHealthMobileClean

# Kill any existing Metro processes
killall node

# Clear cache and restart
npx expo start --clear
```

### Solution 2: Check Metro is Accessible

The iOS Simulator needs to connect to Metro on `localhost:8081`. Make sure:

1. **Metro is running**: You should see Metro bundler output in terminal
2. **Port 8081 is open**: Check with `lsof -i :8081`
3. **No firewall blocking**: macOS firewall might block connections

### Solution 3: Use LAN Host (if localhost doesn't work)

If the simulator can't reach `localhost`, use your Mac's IP:

```bash
# Find your Mac's IP
ifconfig | grep "inet " | grep -v 127.0.0.1

# Start Metro with LAN host
npx expo start --host lan
```

Then rebuild the app so it uses the LAN IP for Metro:

```bash
npx expo run:ios
```

### Solution 4: Reset Simulator Network

Sometimes the simulator's network stack gets confused:

1. **Reset Simulator**:
   - iOS Simulator → Device → Erase All Content and Settings
   - Or: `xcrun simctl erase all`

2. **Restart everything**:
   ```bash
   # Kill Metro
   killall node
   
   # Restart Metro
   cd MentalHealthMobileClean
   npx expo start --clear
   
   # Rebuild app
   npx expo run:ios
   ```

### Solution 5: Check Info.plist for Localhost Exception

Make sure `localhost` is in the ATS exceptions. It should already be there, but verify:

```xml
<key>NSExceptionDomains</key>
<dict>
  <key>localhost</key>
  <dict>
    <key>NSExceptionAllowsInsecureHTTPLoads</key>
    <false/>
    <key>NSExceptionRequiresForwardSecrecy</key>
    <false/>
    <key>NSIncludesSubdomains</key>
    <true/>
  </dict>
</dict>
```

## Step-by-Step Troubleshooting

1. **Check Metro is running**:
   ```bash
   curl http://localhost:8081/status
   ```
   Should return: `{"status":"running"}`

2. **Check Metro in browser**:
   Open: `http://localhost:8081` in Safari
   Should show Metro bundler interface

3. **Check from Simulator**:
   - Open Safari in Simulator
   - Navigate to: `http://localhost:8081`
   - Should load Metro bundler page

4. **If localhost doesn't work from Simulator**:
   - Use LAN host: `npx expo start --host lan`
   - Rebuild app: `npx expo run:ios`

## Common Causes

- ✅ Metro bundler not running
- ✅ Metro bundler crashed
- ✅ Firewall blocking port 8081
- ✅ Simulator network issues
- ✅ Cache corruption
- ✅ Multiple Metro instances running

## Complete Reset

If nothing works, do a complete reset:

```bash
cd MentalHealthMobileClean

# Kill all node processes
killall node

# Clear all caches
rm -rf node_modules/.cache
rm -rf .expo
rm -rf ios/build

# Clear Metro cache
npx expo start --clear

# In another terminal, rebuild
npx expo run:ios
```

## Verify It's Working

After fixing, you should see:
- ✅ Metro bundler shows "Metro waiting on..."
- ✅ App loads without "unsanitizedScriptURLString" error
- ✅ You can see console logs in Metro terminal
- ✅ Hot reload works (save a file, app updates)

