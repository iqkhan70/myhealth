# Building iOS App with Free Apple ID (No Paid Developer Account)

## The Problem
- EAS Build requires a paid Apple Developer account ($99/year) for physical iPhone
- You want the app to work without your Mac running
- Free Apple ID can only install on iPhone for 7 days (then needs re-signing)

## Solutions

### Option 1: Development Build + Expo Updates (Recommended)
Build locally, install on iPhone, use OTA updates for JavaScript changes.

### Option 2: Use iOS Simulator (Free, but Mac only)
Works without paid account, but only on your Mac.

### Option 3: Get Apple Developer Account
$99/year - then you can use EAS Build normally.

---

## Option 1: Development Build + Expo Updates

This lets you:
- ✅ Install on physical iPhone (free Apple ID)
- ✅ Update JavaScript via OTA (no Metro needed after first install)
- ⚠️ App expires after 7 days (need to re-sign)
- ⚠️ First build needs your Mac

### Steps

#### 1. Build Development Build Locally
```bash
cd MentalHealthMobileClean

# Prebuild native code
npx expo prebuild

# Install CocoaPods
cd ios
pod install
cd ..

# Build for your iPhone
npx expo run:ios --device
```

**When prompted:**
- Select your iPhone as target
- Use your free Apple ID for signing
- Trust the developer certificate on iPhone (Settings → General → VPN & Device Management)

#### 2. Configure Expo Updates (OTA Updates)

Update `app.json` to enable OTA updates:

```json
{
  "expo": {
    "updates": {
      "enabled": true,
      "checkAutomatically": "ON_LOAD",
      "fallbackToCacheTimeout": 0
    }
  }
}
```

#### 3. Publish Updates (No Rebuild Needed)

When you change JavaScript code:
```bash
eas update --branch production --message "Update app"
```

The app will auto-update when opened (no Metro needed).

#### 4. Limitations
- ⚠️ App expires after 7 days (free Apple ID limitation)
- ⚠️ Need to rebuild/re-sign every 7 days
- ✅ JavaScript updates work via OTA (no rebuild needed)

---

## Option 2: iOS Simulator (Free, Mac Only)

Works completely free, but only on your Mac:

```bash
cd MentalHealthMobileClean
npx expo run:ios
```

This builds for simulator (free, no account needed), but:
- ❌ Only works on your Mac
- ❌ Can't use on physical iPhone
- ✅ No Metro needed (bundled in simulator build)

---

## Option 3: Get Apple Developer Account

**Best long-term solution:**
- $99/year Apple Developer account
- Then use: `eas build --platform ios --profile production`
- Apps don't expire
- Can use TestFlight
- Can publish to App Store

Sign up: https://developer.apple.com

---

## Recommendation

**For now (free):**
1. Build development build locally: `npx expo run:ios --device`
2. Install on iPhone (free Apple ID)
3. Configure Expo Updates for OTA JavaScript updates
4. Re-sign every 7 days (or get Developer account)

**Long-term:**
- Get Apple Developer account ($99/year)
- Use EAS Build for production builds
- Apps don't expire
- Can use TestFlight

---

## Quick Start (Free Account)

```bash
cd MentalHealthMobileClean

# Build for your iPhone
npx expo run:ios --device

# Follow prompts:
# - Select your iPhone
# - Use free Apple ID for signing
# - Trust certificate on iPhone
```

After installation:
- App works on iPhone
- Connects to `https://caseflowstage.store/api`
- ⚠️ Expires in 7 days (need to rebuild)

---

## Note About Metro

Even with a development build:
- **First install**: Needs Metro for initial JavaScript bundle
- **After install**: If you configure Expo Updates, JavaScript updates via OTA (no Metro)
- **Native changes**: Need to rebuild (but rare)

The app will work on your iPhone, but you'll need to rebuild/re-sign every 7 days with a free Apple ID.

