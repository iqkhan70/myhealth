# Fix iOS Code Signing Error

## The Error
```
No profiles for 'com.mentalhealthapp.mobile.clean' were found: 
Xcode couldn't find any iOS App Development provisioning profiles matching 'com.mentalhealthapp.mobile.clean'. 
Automatic signing is disabled and unable to generate a profile.
```

## Solution: Enable Automatic Signing in Xcode

### Step 1: Open Project in Xcode
The workspace should already be opening. If not:
```bash
cd MentalHealthMobileClean
open ios/MentalHealthMobile.xcworkspace
```

### Step 2: Configure Signing

1. **In Xcode:**
   - Click on **"MentalHealthMobile"** project (blue icon) in the left sidebar
   - Select **"MentalHealthMobile"** target
   - Click **"Signing & Capabilities"** tab

2. **Enable Automatic Signing:**
   - ✅ Check **"Automatically manage signing"**
   - Select your **Team** (your Apple ID - free account works)
   - Xcode will automatically create a provisioning profile

3. **If you see "No accounts found":**
   - Click **"Add Account..."**
   - Sign in with your Apple ID
   - Go back to Signing & Capabilities
   - Select your team

### Step 3: Build Again

After enabling automatic signing, close Xcode and run:

```bash
cd MentalHealthMobileClean
npx expo run:ios --device
```

## Alternative: Use Simulator (No Signing Needed)

If you just want to test without dealing with signing:

```bash
cd MentalHealthMobileClean
npx expo run:ios
```

This builds for iOS Simulator (no signing required), but:
- ✅ Works immediately
- ❌ Only runs on your Mac (not physical iPhone)
- ✅ No 7-day expiration

## Quick Fix Summary

1. Open Xcode: `open ios/MentalHealthMobile.xcworkspace`
2. Enable "Automatically manage signing"
3. Select your Apple ID team
4. Run: `npx expo run:ios --device`

The workspace should be opening now - follow the steps above!

