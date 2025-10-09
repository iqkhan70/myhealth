# Mental Health Mobile App - Clean Version

This is a clean, fresh version of the Mental Health Mobile App built with Expo and React Native, designed to work seamlessly across iOS, Android, and Web platforms.

## ✅ What's Fixed

- **No SDK version mismatches** - All dependencies are compatible
- **Clean dependency tree** - No conflicting packages
- **Consistent environment** - Works on Expo Go, simulators, and web
- **Proper error handling** - Graceful fallbacks for all scenarios
- **Network configuration** - Correctly configured for local development

## 🚀 Quick Start

### Prerequisites

- Node.js (v16 or higher)
- Expo CLI (`npm install -g @expo/cli`)
- Expo Go app on your phone (for testing)

### Installation

```bash
cd /Users/mohammedkhan/iq/health/MentalHealthMobileClean
npm install
```

### Running the App

#### Option 1: Use the startup script (Recommended)

```bash
./start-mobile-clean.sh
```

#### Option 2: Manual start

```bash
# For mobile (iOS/Android)
npx expo start --clear --host lan --port 8081

# For web only
npx expo start --web --port 8083
```

## 📱 Testing on Different Platforms

### 1. **Expo Go (Phone)**

- Run the startup script
- Scan the QR code with Expo Go app
- Login with: `john@doe.com` / `demo123`

### 2. **iOS Simulator**

- Run the startup script
- Press `i` in the terminal
- Requires Xcode installed

### 3. **Android Emulator**

- Run the startup script
- Press `a` in the terminal
- Requires Android Studio setup

### 4. **Web Browser**

- Run the startup script
- Press `w` in the terminal
- Or visit: `http://localhost:8081`

## 🔧 Features

### ✅ Working Features

- **Login/Logout** - Secure authentication
- **Contacts List** - View assigned doctors/patients
- **Chat Messaging** - Real-time text communication
- **Call Simulation** - Audio/Video call UI (demo mode)
- **Cross-platform** - Works on iOS, Android, and Web

### 🎯 Call Functionality

- **Audio Calls** - Simulated audio calling with UI
- **Video Calls** - Simulated video calling with UI
- **Call Controls** - Mute, video toggle, end call
- **Real-time Notifications** - Calls notify web app via SignalR

## 🌐 Network Configuration

The app automatically detects the environment and uses the correct API endpoints:

- **Desktop Browser**: `http://192.168.86.113:5262/api`
- **Mobile/Expo Go**: `http://192.168.86.113:5262/api`

## 🐛 Troubleshooting

### "EMFILE: too many open files"

```bash
ulimit -n 65536
export WATCHMAN_NO_LOCAL=1
```

### Metro bundler issues

```bash
npx expo start --clear
```

### Network connection issues

- Ensure server is running on `192.168.86.113:5262`
- Check firewall settings
- Verify both devices are on same network

## 📦 Dependencies

### Core Dependencies

- `expo`: ~54.0.12
- `react`: 19.1.0
- `react-native`: 0.81.4
- `react-native-web`: ~0.21.0

### Additional Packages

- `@react-native-async-storage/async-storage`: 2.1.0

## 🔄 Development Workflow

1. **Start the server** (in another terminal):

   ```bash
   cd /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Server
   dotnet run --urls "http://0.0.0.0:5262;https://0.0.0.0:5443"
   ```

2. **Start the mobile app**:

   ```bash
   cd /Users/mohammedkhan/iq/health/MentalHealthMobileClean
   ./start-mobile-clean.sh
   ```

3. **Test the flow**:
   - Login on mobile app
   - Make a call to a contact
   - Check web app receives notification
   - Accept call on web app
   - Verify call interface works

## 🎉 Success Criteria

When everything is working correctly, you should be able to:

1. ✅ **Login** on mobile without errors
2. ✅ **See contacts** list populated
3. ✅ **Make calls** that show up on web app
4. ✅ **Accept calls** on web app
5. ✅ **Use call interface** with controls
6. ✅ **Send/receive messages** in chat

## 📞 Support

If you encounter any issues:

1. Check the server is running on port 5262
2. Verify network connectivity
3. Clear Metro cache with `--clear` flag
4. Restart Expo development server

The app is now clean, consistent, and ready for development! 🎉
