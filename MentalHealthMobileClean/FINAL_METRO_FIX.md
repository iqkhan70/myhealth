# Final Fix: Metro Connection Issue

## The Root Cause

You have **multiple Metro instances** running, which causes the app to connect to the wrong instance or get confused about which Metro to use.

## ✅ Complete Fix (Do This)

### Step 1: Run the Complete Fix Script

```bash
cd MentalHealthMobileClean
./COMPLETE_FIX_METRO.sh
```

This script will:
1. Kill ALL Metro/Expo processes
2. Clear all caches
3. Optionally reset simulator
4. Start a SINGLE Metro instance
5. Wait until Metro is FULLY ready (not just responding, but serving bundles)
6. Verify everything is working

**Follow the prompts** - it will ask if you want to reset the simulator (recommended: yes)

### Step 2: Verify Metro is Accessible from Simulator

**In the Simulator:**
1. Open Safari
2. Go to: `http://localhost:8081`
3. Should show Metro bundler interface

If Safari can't load it, there's a network issue.

### Step 3: Run the App (NEW Terminal)

**Open a NEW terminal** (keep Metro running in the first terminal):

```bash
cd MentalHealthMobileClean
npx expo run:ios
```

The app should now connect successfully!

## 🔍 Why This Happens

1. **Multiple Metro instances**: When you run `npx expo run:ios` multiple times, it can start multiple Metro instances
2. **Timing issue**: App launches before Metro is fully ready to serve bundles
3. **Bundle URL confusion**: App tries to connect to wrong Metro instance

## 🐛 If Still Not Working

### Option 1: Manual Complete Reset

```bash
cd MentalHealthMobileClean

# Kill everything
killall node
lsof -ti:8081 | xargs kill -9 2>/dev/null || true

# Clear everything
rm -rf .expo node_modules/.cache ios/build
rm -rf ~/Library/Caches/com.apple.dt.Xcode

# Reset simulator
xcrun simctl erase all

# Start Metro (wait for "Metro waiting on...")
npx expo start --clear --localhost

# Wait 10 seconds after Metro is ready
# Then in NEW terminal: npx expo run:ios
```

### Option 2: Check from Simulator Safari

If Safari in Simulator can't access `http://localhost:8081`, there's a network issue:

1. **Check firewall**: macOS System Settings → Firewall
2. **Try 127.0.0.1**: In Safari, try `http://127.0.0.1:8081`
3. **Reset simulator network**: `xcrun simctl erase all`

### Option 3: Use Development Build Instead

If Metro keeps having issues, you can build a development build with Metro embedded:

```bash
cd MentalHealthMobileClean
npx expo run:ios --no-build-cache
```

## ✅ Success Checklist

- [ ] Only ONE Metro instance running (check: `pgrep -f "expo start" | wc -l` should be 1)
- [ ] Metro accessible: `curl http://localhost:8081/status` works
- [ ] Bundle accessible: `curl "http://localhost:8081/index.bundle?platform=ios&dev=true"` works
- [ ] Safari in Simulator can load `http://localhost:8081`
- [ ] App launches and connects successfully
- [ ] No "unsanitizedScriptURLString" error

## 💡 Key Points

1. **Always kill all Metro instances first** - Multiple instances cause issues
2. **Wait for Metro to be FULLY ready** - Not just responding, but serving bundles
3. **Use two terminals** - One for Metro, one for running the app
4. **Verify from Simulator Safari** - If Safari can't access Metro, the app won't be able to either

## 🎯 Quick Reference

**The Right Way:**
```bash
# Terminal 1
./COMPLETE_FIX_METRO.sh
# Wait for "Metro is fully ready!"

# Terminal 2 (after Metro is ready)
npx expo run:ios
```

**The Wrong Way:**
```bash
# This can start multiple Metro instances
npx expo run:ios  # ❌ May start Metro but app launches too early
```

