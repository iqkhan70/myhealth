# Fix: "unsanitizedScriptURLString = (null)" on Simulator

## The Problem

You're getting this error because the app launches **before Metro bundler is ready**. The simulator can't find the JavaScript bundle.

## ✅ Solution: Start Metro First

**The key is: Metro must be running BEFORE you run `npx expo run:ios`**

### Method 1: Use the Helper Script (Easiest)

```bash
cd MentalHealthMobileClean
./start-and-run-ios.sh
```

This script:
1. Kills any existing Metro processes
2. Starts Metro and waits for it to be ready
3. Then launches the iOS app

### Method 2: Manual Two-Terminal Approach

**Terminal 1 - Start Metro:**
```bash
cd MentalHealthMobileClean

# Kill existing Metro
pkill -f "expo start" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true

# Start Metro for simulator
npx expo start --clear --localhost
```

**Wait until you see:** `Metro waiting on exp://localhost:8081`

**Then in Terminal 2:**
```bash
cd MentalHealthMobileClean
npx expo run:ios
```

### Method 3: Verify Metro is Running First

Before running the app, verify Metro is accessible:

```bash
# Check Metro status
curl http://localhost:8081/status

# Should return: {"status":"running"}
```

If this fails, Metro isn't ready yet. Wait a bit longer.

## 🔍 Why This Happens

When you run `npx expo run:ios`, it:
1. Builds/launches the app
2. App immediately tries to connect to Metro on `localhost:8081`
3. If Metro isn't ready → Error!

**Solution:** Start Metro first, wait for it to be ready, THEN launch the app.

## 🐛 Troubleshooting

### Metro won't start?

```bash
# Kill everything
killall node
lsof -ti:8081 | xargs kill -9 2>/dev/null || true

# Clear caches
cd MentalHealthMobileClean
rm -rf .expo node_modules/.cache

# Try starting Metro again
npx expo start --clear --localhost
```

### Metro starts but app still can't connect?

1. **Check Metro is on localhost:**
   ```bash
   curl http://localhost:8081/status
   ```

2. **Check from Simulator Safari:**
   - Open Safari in Simulator
   - Go to: `http://localhost:8081`
   - Should show Metro bundler interface

3. **Verify Metro started with `--localhost`:**
   - Check the Metro terminal output
   - Should say: `Metro waiting on exp://localhost:8081`
   - NOT: `exp://192.168.x.x:8081`

### Still not working?

**Complete reset:**
```bash
cd MentalHealthMobileClean

# Kill everything
killall node
lsof -ti:8081 | xargs kill -9 2>/dev/null || true

# Clear all caches
rm -rf .expo node_modules/.cache ios/build

# Reset simulator
xcrun simctl erase all

# Start Metro
npx expo start --clear --localhost

# Wait for "Metro waiting on..." message
# Then in another terminal: npx expo run:ios
```

## ✅ Success Checklist

- [ ] Metro is running (you see "Metro waiting on..." in terminal)
- [ ] Metro accessible: `curl http://localhost:8081/status` returns `{"status":"running"}`
- [ ] Metro started with `--localhost` flag (for simulator)
- [ ] App launched AFTER Metro was ready
- [ ] App loads without "unsanitizedScriptURLString" error
- [ ] Console logs appear in Metro terminal

## 💡 Pro Tips

1. **Always start Metro first** - Use two terminals or the helper script
2. **Wait for Metro to be ready** - Look for "Metro waiting on..." message
3. **Keep Metro running** - Don't close the Metro terminal while using the app
4. **Use the helper script** - `./start-and-run-ios.sh` handles everything automatically

