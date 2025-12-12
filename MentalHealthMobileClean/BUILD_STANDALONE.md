# Building a Standalone App (No Metro Required)

## Problem
Expo Go requires Metro bundler to be running on your Mac. To use the app without your Mac, you need to build a **standalone production app** that bundles all JavaScript code.

## Solution: EAS Build

EAS (Expo Application Services) will build a standalone app that:
- ✅ Works without Metro bundler
- ✅ Works without your Mac running
- ✅ Connects directly to your DigitalOcean server
- ✅ Includes all JavaScript code bundled

## Prerequisites

1. **Install EAS CLI**:
   ```bash
   npm install -g eas-cli
   ```

2. **Login to Expo**:
   ```bash
   eas login
   ```

3. **Configure EAS** (already done - `eas.json` exists):
   - ✅ `eas.json` is configured
   - ✅ Project ID is set in `app.json`

## Build Commands

### For Android (APK - can install directly):
```bash
cd MentalHealthMobileClean
eas build --platform android --profile production
```

This will:
- Build the app in the cloud
- Create an APK file
- Give you a download link
- Install the APK on your Android device

### For iOS (requires Apple Developer account):
```bash
cd MentalHealthMobileClean
eas build --platform ios --profile production
```

**Note**: iOS builds require:
- Apple Developer account ($99/year)
- Or use TestFlight for free testing

## After Building

1. **Download the APK** (Android) from the EAS dashboard
2. **Install on your device**:
   - Android: Transfer APK to phone → Open → Install
   - iOS: Install via TestFlight or App Store
3. **Use the app** - No Metro bundler needed!

## Alternative: Local Build (Faster, but requires setup)

If you want to build locally instead of in the cloud:

### Android:
```bash
cd MentalHealthMobileClean
npx expo prebuild
cd android
./gradlew assembleRelease
# APK will be in: android/app/build/outputs/apk/release/app-release.apk
```

### iOS:
```bash
cd MentalHealthMobileClean
npx expo prebuild
cd ios
xcodebuild -workspace MentalHealthMobile.xcworkspace -scheme MentalHealthMobile -configuration Release
```

## Quick Start (Recommended)

1. **Install EAS CLI**:
   ```bash
   npm install -g eas-cli
   eas login
   ```

2. **Build Android APK**:
   ```bash
   cd MentalHealthMobileClean
   eas build --platform android --profile production
   ```

3. **Wait for build** (5-15 minutes)

4. **Download and install APK** on your Android device

5. **Done!** App works without your Mac running.

## Notes

- **First build**: Takes 10-15 minutes
- **Subsequent builds**: Faster (5-10 minutes) due to caching
- **Updates**: If you change JavaScript code, you can use OTA updates (Expo Updates) without rebuilding
- **Native changes**: Require a new build

## Troubleshooting

### "EAS not found"
```bash
npm install -g eas-cli
```

### "Not logged in"
```bash
eas login
```

### "Project ID not found"
The project ID is already set in `app.json` (`mental-health-mobile-clean`). If you get an error, run:
```bash
eas init
```

