# Expo Go Limitations - Agora Video/Audio Calls

## ⚠️ Important Notice

The app uses `react-native-agora` for video and audio calls, which **requires native code** and **does NOT work in Expo Go**.

## Current Status

✅ **App will run in Expo Go** - The app has been updated to handle missing Agora gracefully
❌ **Video/Audio calls will NOT work** - You'll see an error message when trying to make calls

## Solutions

### Option 1: Create a Development Build (Recommended)

To enable video/audio calls, you need to create a development build:

#### For iOS:
```bash
cd MentalHealthMobileClean
npx expo run:ios
```

#### For Android:
```bash
cd MentalHealthMobileClean
npx expo run:android
```

This will:
- Build the native app with Agora support
- Install it on your device/simulator
- Enable video and audio calls

### Option 2: Use Expo Go (Limited Functionality)

If you want to test other features (login, messages, documents, emergency) without calls:

```bash
cd MentalHealthMobileClean
npx expo start
# Then scan QR code with Expo Go app
```

**What works in Expo Go:**
- ✅ Login/Authentication
- ✅ Messages/Chat
- ✅ Document Upload
- ✅ Emergency Alerts
- ✅ SMS
- ❌ Video Calls (will show error)
- ❌ Audio Calls (will show error)

## Error Message

When you try to make a call in Expo Go, you'll see:
```
Calls Not Available
Video/Audio calls require a development build. 
Agora is not available in Expo Go.

To enable calls, run:
npx expo run:ios
or
npx expo run:android
```

## Why This Happens

`react-native-agora` is a native module that requires:
- Native iOS code (Objective-C/Swift)
- Native Android code (Java/Kotlin)
- These are compiled into the app binary

Expo Go is a pre-built app that doesn't include your custom native modules.

## Development Build vs Expo Go

| Feature | Expo Go | Development Build |
|---------|---------|-------------------|
| Quick Testing | ✅ Fast | ⚠️ Slower (needs build) |
| Native Modules | ❌ No | ✅ Yes |
| Video Calls | ❌ No | ✅ Yes |
| Audio Calls | ❌ No | ✅ Yes |
| Other Features | ✅ Yes | ✅ Yes |

## Next Steps

1. **For testing calls**: Create a development build (`npx expo run:ios` or `npx expo run:android`)
2. **For testing other features**: Use Expo Go (current setup)
3. **For production**: Build a production app with `eas build` or `expo build`

