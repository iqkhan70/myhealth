# Building Standalone iOS App (No Metro Required)

## Problem
Expo Go requires Metro bundler running on your Mac. To use the app without your Mac, you need a **standalone production app**.

## Solution Options

### Option 1: EAS Build (Cloud - Recommended)
Builds in the cloud, no Xcode needed on your Mac.

### Option 2: Local Build (Faster, but requires Xcode)
Build locally if you have Xcode installed.

---

## Option 1: EAS Build (Cloud)

### Prerequisites
1. **Apple Developer Account** ($99/year) - Required for device installation
   - OR use **TestFlight** (free, but still needs Apple Developer account)
   - OR use **iOS Simulator** (free, but only works on Mac)

2. **EAS CLI** (already installed):
   ```bash
   eas login
   ```

### Build Command
```bash
cd MentalHealthMobileClean
eas build --platform ios --profile production
```

### What Happens
1. **First time**: EAS will ask about credentials
   - Choose: "Let EAS manage your credentials" (recommended)
   - EAS handles certificates and provisioning profiles

2. **Build starts**: 
   - Builds in the cloud (10-20 minutes)
   - You'll see progress in terminal

3. **Download**:
   - For **TestFlight**: App is uploaded automatically
   - For **Direct Install**: Download IPA file
   - Install via TestFlight or Xcode

### Installing on Your iPhone

#### Via TestFlight (Easiest):
1. Build completes → App uploaded to TestFlight
2. Install TestFlight app on iPhone
3. Accept TestFlight invitation email
4. Install app from TestFlight

#### Via Direct Install:
1. Download IPA from EAS dashboard
2. Install via Xcode or Apple Configurator
3. Trust the developer certificate on iPhone

---

## Option 2: Local Build (Faster)

### Prerequisites
- ✅ Xcode installed (from App Store)
- ✅ Xcode Command Line Tools: `xcode-select --install`
- ✅ Apple Developer account (free account works for simulator)

### Build Steps

#### 1. Prebuild Native Code
```bash
cd MentalHealthMobileClean
npx expo prebuild
```

#### 2. Install CocoaPods Dependencies
```bash
cd ios
pod install
cd ..
```

#### 3. Build for Device (Physical iPhone)
```bash
# Open in Xcode
open ios/MentalHealthMobile.xcworkspace

# In Xcode:
# 1. Select your iPhone as target device
# 2. Select your Apple Developer team (Signing & Capabilities)
# 3. Product → Archive
# 4. Distribute App → Ad Hoc or Development
# 5. Export IPA
```

#### 4. Build for Simulator (Free, Mac only)
```bash
npx expo run:ios
# This still needs Metro, but works on simulator
```

---

## Quick Start (EAS Build - Recommended)

1. **Login to EAS**:
   ```bash
   eas login
   ```

2. **Build iOS app**:
   ```bash
   cd MentalHealthMobileClean
   eas build --platform ios --profile production
   ```

3. **Wait for build** (10-20 minutes)

4. **Install on iPhone**:
   - Via TestFlight (easiest)
   - Or download IPA and install via Xcode

5. **Done!** App works without your Mac running.

---

## Important Notes

### Apple Developer Account
- **Required** for installing on physical iPhone
- **Free account** works for iOS Simulator only
- **$99/year** for App Store/TestFlight/Direct install

### TestFlight (Free with Developer Account)
- Best option for testing
- Easy installation on iPhone
- Can share with testers
- Apps expire after 90 days (need to rebuild)

### Standalone App Benefits
- ✅ Works without Metro bundler
- ✅ Works without your Mac running
- ✅ Connects to `https://caseflowstage.store/api`
- ✅ All features work (login, chat, calls, etc.)

---

## Troubleshooting

### "Not logged in to EAS"
```bash
eas login
```

### "Apple Developer account required"
- Sign up at: https://developer.apple.com
- $99/year for full access
- Free account works for simulator only

### "Code signing error"
- Let EAS manage credentials (recommended)
- Or configure manually in Xcode

### "Build failed"
Check EAS dashboard: https://expo.dev/accounts/[your-account]/projects/mental-health-mobile-clean/builds

---

## Current Configuration

- ✅ **Server**: `caseflowstage.store` (staging)
- ✅ **Port**: `443` (HTTPS)
- ✅ **API URL**: `https://caseflowstage.store/api`
- ✅ **iOS Bundle ID**: `com.mentalhealthapp.mobile.clean`
- ✅ **Project ID**: `292709e6-41ad-4ecc-917f-813e8388f9b8`

Ready to build! 🚀

