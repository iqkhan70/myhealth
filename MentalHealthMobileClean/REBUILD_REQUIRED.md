# ⚠️ REBUILD REQUIRED

## Why the "Network request failed" error?

The certificate bypass configuration we just added requires a **native rebuild** of the app. The changes to:
- `android/app/src/main/AndroidManifest.xml` 
- `android/app/src/main/res/xml/network_security_config.xml`
- `app.json` (iOS settings)

These are **native platform configurations** that are only applied when you build the app, not when you just reload it in Expo Go.

## 🔧 Solution: Rebuild the App

### For Android:
```bash
cd MentalHealthMobileClean
npx expo run:android
```

### For iOS:
```bash
cd MentalHealthMobileClean
npx expo run:ios
```

### Important Notes:
- **Expo Go won't work** - You need a development build because we modified native files
- The rebuild will take a few minutes the first time
- After rebuilding, the app will be able to connect to HTTPS with self-signed certificates

## ✅ After Rebuilding

Once rebuilt, the app should:
- ✅ Connect to `https://192.168.86.25:5262` without certificate errors
- ✅ Login successfully
- ✅ Make API calls over HTTPS
- ✅ Support Agora video/audio calls (requires HTTPS)

## 🧪 Quick Test

After rebuilding, try logging in again. The "Network request failed" error should be gone!

