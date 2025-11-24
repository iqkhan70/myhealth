# Development Build Guide

## What is a Development Build?

A **development build** is a custom version of your app that includes:
- ✅ Your native code (certificate bypass config, Agora, etc.)
- ✅ All your custom native modules
- ✅ Ability to connect to HTTPS with self-signed certificates
- ✅ Hot reloading and fast refresh (like Expo Go)

**Expo Go** is a pre-built app that doesn't include your custom native code, so it won't work with:
- Certificate bypass configuration
- Agora video/audio calls
- Other custom native modules

## Prerequisites

### For Android:
- ✅ Android Studio installed
- ✅ Android SDK installed
- ✅ Android device connected OR Android emulator running
- ✅ Java Development Kit (JDK)

### For iOS (macOS only):
- ✅ Xcode installed (from App Store)
- ✅ Xcode Command Line Tools: `xcode-select --install`
- ✅ iOS Simulator OR physical iOS device
- ✅ Apple Developer account (free account works for simulator)

## Step-by-Step Instructions

### Option 1: Android Development Build

#### 1. Check Prerequisites
```bash
# Check if Android SDK is installed
echo $ANDROID_HOME

# If not set, you may need to set it in ~/.zshrc or ~/.bash_profile
# export ANDROID_HOME=$HOME/Library/Android/sdk
```

#### 2. Start Android Emulator (or connect device)
```bash
# Option A: Start emulator from Android Studio
# Open Android Studio → Tools → Device Manager → Start emulator

# Option B: Start from command line (if you know the AVD name)
emulator -avd Pixel_5_API_33
```

#### 3. Build and Install
```bash
cd MentalHealthMobileClean

# Build and install on connected device/emulator
npx expo run:android
```

**What happens:**
- First time: Downloads dependencies, builds native code (5-10 minutes)
- Subsequent builds: Faster (1-3 minutes)
- Installs the app on your device/emulator
- Starts Metro bundler automatically

#### 4. Use the App
- The app will open automatically
- You can reload with `r` in the terminal
- Shake device/emulator for developer menu

---

### Option 2: iOS Development Build

#### 1. Check Prerequisites
```bash
# Check if Xcode is installed
xcode-select -p

# If not, install Xcode from App Store first
```

#### 2. Install CocoaPods (if not already installed)
```bash
# Install CocoaPods
sudo gem install cocoapods

# Navigate to iOS directory and install pods
cd MentalHealthMobileClean/ios
pod install
cd ..
```

#### 3. Start iOS Simulator (or connect device)
```bash
# Option A: Start from Xcode
# Xcode → Open Developer Tool → Simulator

# Option B: Start from command line
open -a Simulator
```

#### 4. Build and Install
```bash
cd MentalHealthMobileClean

# Build and install on simulator/device
npx expo run:ios
```

**What happens:**
- First time: Downloads dependencies, builds native code (10-15 minutes)
- Subsequent builds: Faster (2-5 minutes)
- Installs the app on your simulator/device
- Starts Metro bundler automatically

#### 5. Use the App
- The app will open automatically in simulator
- You can reload with `r` in the terminal
- Press `Cmd+D` for developer menu

---

## Troubleshooting

### Android: "SDK location not found"
```bash
# Set ANDROID_HOME in your shell profile (~/.zshrc or ~/.bash_profile)
export ANDROID_HOME=$HOME/Library/Android/sdk
export PATH=$PATH:$ANDROID_HOME/emulator
export PATH=$PATH:$ANDROID_HOME/platform-tools
export PATH=$PATH:$ANDROID_HOME/tools
export PATH=$PATH:$ANDROID_HOME/tools/bin

# Then reload
source ~/.zshrc  # or source ~/.bash_profile
```

### Android: "Gradle build failed"
```bash
# Clean and rebuild
cd MentalHealthMobileClean/android
./gradlew clean
cd ..
npx expo run:android
```

### iOS: "CocoaPods not found"
```bash
# Install CocoaPods
sudo gem install cocoapods

# Install pods
cd MentalHealthMobileClean/ios
pod install
cd ..
```

### iOS: "Code signing error"
- For simulator: Usually works automatically
- For physical device: You need to sign in with Apple ID in Xcode
  - Xcode → Preferences → Accounts → Add Apple ID
  - Select your team in project settings

### Both: "Metro bundler not starting"
```bash
# Start Metro manually
cd MentalHealthMobileClean
npx expo start

# Then in another terminal, run the build command
```

---

## After Building

### Development Workflow

1. **First build**: Takes longer (5-15 minutes)
2. **Subsequent changes**: 
   - JavaScript changes: Hot reload automatically
   - Native config changes: Need to rebuild (`npx expo run:android` or `npx expo run:ios`)
   - App config changes: Usually hot reload works

### Testing Certificate Bypass

After building, test the HTTPS connection:

1. Make sure server is running: `dotnet run --launch-profile https`
2. Open the app (should open automatically after build)
3. Try to login - should work without "Network request failed" error
4. Check console logs for any SSL warnings

### Updating Server IP

If your server IP changes:

1. Update `src/config/app.config.js` (JavaScript - hot reload works)
2. Update `android/app/src/main/res/xml/network_security_config.xml` (Android - needs rebuild)
3. Update `app.json` iOS ATS settings (iOS - needs rebuild)
4. Rebuild: `npx expo run:android` or `npx expo run:ios`

---

## Quick Reference

### Build Commands
```bash
# Android
npx expo run:android

# iOS
npx expo run:ios

# Start Metro bundler separately
npx expo start
```

### Useful Commands During Development
```bash
# Reload app (in Metro terminal)
r - Reload
R - Reload and clear cache
m - Toggle menu
j - Open debugger
```

### Clean Build (if things go wrong)
```bash
# Android
cd android
./gradlew clean
cd ..

# iOS
cd ios
rm -rf build Pods Podfile.lock
pod install
cd ..

# Then rebuild
npx expo run:android  # or run:ios
```

---

## Next Steps

1. ✅ Build the app using instructions above
2. ✅ Test login with HTTPS server
3. ✅ Verify certificate bypass is working
4. ✅ Test Agora video/audio calls (if needed)

The app should now connect to `https://192.168.86.25:5262` without certificate errors!

