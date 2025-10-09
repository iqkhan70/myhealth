# Expo Development Build Setup Instructions

## 🏗️ Development Build vs Expo Go

**Expo Go**: Limited to Expo SDK modules only
**Development Build**: Supports ANY native modules (including react-native-webrtc)

## 📱 Building for Different Platforms

### iOS Development Build

```bash
# Local build (requires Xcode)
npx expo run:ios --device

# Cloud build (no Xcode required)
eas build --platform ios --profile development
```

### Android Development Build

```bash
# Local build (requires Android Studio)
npx expo run:android --device

# Cloud build (no Android Studio required)
eas build --platform android --profile development
```

## 🔧 What We've Set Up

1. ✅ **App Configuration**: Updated `app.json` with development build settings
2. ✅ **EAS Configuration**: Created `eas.json` for cloud builds
3. ✅ **Native Module**: Reinstalled `react-native-webrtc`
4. ✅ **Dev Client**: Added `expo-dev-client` plugin
5. ✅ **Smart Detection**: WebRTC service detects build type automatically

## 🎯 Next Steps

### Option A: Local Build (Faster, requires dev tools)

1. **iOS**: Ensure Xcode is installed
2. **Android**: Ensure Android Studio is installed
3. Run the build command above
4. Install on your device

### Option B: Cloud Build (Slower, no dev tools needed)

1. Create Expo account: https://expo.dev/signup
2. Login: `eas login`
3. Configure project: `eas build:configure`
4. Build: `eas build --platform [ios|android] --profile development`
5. Download and install the APK/IPA

## 🧪 Testing Real WebRTC

Once you have the development build installed:

1. **Install Development Build** on your device
2. **Start Development Server**: `npx expo start --dev-client`
3. **Scan QR Code** with your development build app
4. **Login** and test audio/video calls
5. **Check Logs** for: "✅ Real WebRTC Native: react-native-webrtc loaded successfully"

## 🔍 How to Verify It's Working

**Development Build Logs:**

```
📱 Real WebRTC Native: Development build detected, loading react-native-webrtc
✅ Real WebRTC Native: react-native-webrtc loaded successfully
```

**Expo Go Logs (fallback):**

```
📱 Real WebRTC Native: Using Expo Go optimized WebRTC
📱 Expo Go WebRTC: Using enhanced simulation for mobile
```

## 📋 Troubleshooting

- **Build fails**: Check iOS/Android development environment setup
- **No audio/video**: Ensure permissions are granted on device
- **Module not found**: Verify development build installation vs Expo Go

The service will automatically detect and use real WebRTC in development builds!
