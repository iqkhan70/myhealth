# Build Instructions - Standalone Android App

## ✅ Fixed Issues
- ✅ Project ID updated to valid UUID: `292709e6-41ad-4ecc-917f-813e8388f9b8`
- ✅ EAS configuration updated
- ✅ Server config points to `caseflowstage.store:443`

## 🚀 Build Command

Run this in your terminal:

```bash
cd MentalHealthMobileClean
eas build --platform android --profile production
```

## What Will Happen

1. **First time**: EAS will ask you to set up Android credentials
   - Choose: "Generate a new keystore" (recommended)
   - EAS will manage the keystore for you
   - This is a one-time setup

2. **Build starts**: 
   - Builds in the cloud (5-15 minutes)
   - You'll see progress in the terminal

3. **Download link**: 
   - EAS provides a download link when done
   - Download the APK to your computer
   - Transfer to your Android device
   - Install (enable "Install from unknown sources" if needed)

## Alternative: Local Build (Faster)

If you want to build locally instead:

```bash
cd MentalHealthMobileClean

# Prebuild native code
npx expo prebuild

# Build Android APK
cd android
./gradlew assembleRelease

# APK location:
# android/app/build/outputs/apk/release/app-release.apk
```

Then:
1. Transfer APK to your phone
2. Install it
3. Done! Works without your Mac

## After Installation

- ✅ App works without Metro bundler
- ✅ App works without your Mac running
- ✅ Connects to `https://caseflowstage.store/api`
- ✅ All features work (login, chat, calls, etc.)

## Troubleshooting

### "Not logged in"
```bash
eas login
```

### "Credentials error"
Run the build command interactively (without `--non-interactive`) and follow the prompts.

### "Build failed"
Check the EAS dashboard: https://expo.dev/accounts/[your-account]/projects/mental-health-mobile-clean/builds

