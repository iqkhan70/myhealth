# Mental Health Mobile App

A React Native mobile application for real-time communication between doctors and patients, featuring video/audio calling and chat functionality similar to Microsoft Teams.

## 🚀 Features

### For Doctors

- **Patient List**: View all assigned patients with online status
- **Video Calling**: Initiate video calls with patients
- **Audio Calling**: Start audio-only calls for consultations
- **Real-time Chat**: Send messages to patients instantly
- **Profile Management**: View and manage doctor profile

### For Patients

- **Doctor List**: View all assigned doctors with availability status
- **Call Requests**: Request video/audio calls with doctors
- **Real-time Chat**: Communicate with doctors via text
- **Profile Management**: View and manage patient profile

### Technical Features

- **WebRTC Integration**: High-quality video/audio calling
- **Real-time Communication**: Socket.IO for instant messaging
- **JWT Authentication**: Secure login and session management
- **Cross-platform**: Works on both iOS and Android
- **Teams-like Interface**: Familiar and intuitive UI design

## 📱 Screenshots

_Screenshots will be added after testing_

## 🛠 Installation

### Prerequisites

- Node.js (v16 or higher)
- npm or yarn
- Expo CLI
- iOS Simulator (for iOS development)
- Android Studio (for Android development)

### Setup Instructions

1. **Clone the repository**

   ```bash
   cd /Users/mohammedkhan/iq/health/MentalHealthMobile
   ```

2. **Install dependencies**

   ```bash
   npm install
   # or
   yarn install
   ```

3. **Configure API endpoint**
   Update the API base URL in the following files:

   - `src/services/authService.js`
   - `src/services/patientService.js`
   - `src/services/doctorService.js`
   - `src/context/SocketContext.js`

   Change `http://192.168.86.113:5262` to your server's IP address.

4. **Start the development server**

   ```bash
   npm start
   # or
   yarn start
   ```

5. **Run on device/simulator**
   - For iOS: `npm run ios` or scan QR code with Expo Go
   - For Android: `npm run android` or scan QR code with Expo Go

## 🔧 Configuration

### Server Requirements

The mobile app requires the Mental Health Server to be running with the following endpoints:

- Authentication: `/api/auth/login`
- Mobile API: `/api/mobile/*`
- SignalR Hub: `/mobilehub`

### Environment Variables

No environment variables are required for the mobile app, but ensure the server is configured with:

- JWT authentication
- SignalR support
- CORS enabled for mobile origins

## 🏗 Architecture

### App Structure

```
src/
├── components/          # Reusable UI components
├── context/            # React Context providers
│   ├── AuthContext.js  # Authentication state management
│   └── SocketContext.js # Real-time communication
├── screens/            # App screens/pages
│   ├── LoginScreen.js
│   ├── DoctorHomeScreen.js
│   ├── PatientHomeScreen.js
│   ├── ChatScreen.js
│   ├── VideoCallScreen.js
│   ├── AudioCallScreen.js
│   └── ProfileScreen.js
├── services/           # API services
│   ├── authService.js
│   ├── patientService.js
│   └── doctorService.js
└── utils/              # Utility functions
```

### Navigation Structure

- **Authentication Flow**: Login → Role-based navigation
- **Doctor Flow**: Patient List → Chat/Call screens
- **Patient Flow**: Doctor List → Chat/Call screens
- **Shared**: Profile, Settings

### Real-time Communication

- **Socket.IO**: Real-time messaging and call signaling
- **WebRTC**: Peer-to-peer video/audio communication
- **SignalR Hub**: Server-side real-time communication hub

## 🔐 Authentication

The app uses JWT-based authentication with role-based access:

- **Patients** (Role ID: 1): Can view assigned doctors and initiate communication
- **Doctors** (Role ID: 2): Can view assigned patients and respond to communication
- **Admins** (Role ID: 3): Full system access (not implemented in mobile app)

### Demo Credentials

- **Doctor**: `dr.sarah@mentalhealth.com` / `doctor123`
- **Patient**: `john.doe@email.com` / `patient123`

## 📞 Calling Features

### Video Calls

- HD video quality using WebRTC
- Camera toggle (on/off)
- Microphone mute/unmute
- Call duration tracking
- End call functionality

### Audio Calls

- High-quality audio using WebRTC
- Microphone mute/unmute
- Speaker toggle
- Call duration tracking
- Visual call interface with animations

### Call Flow

1. User initiates call from patient/doctor list or chat
2. Real-time invitation sent via SignalR
3. Target user receives call notification
4. WebRTC peer connection established
5. Media streams exchanged
6. Call controls available during session

## 💬 Chat Features

### Real-time Messaging

- Instant message delivery via Socket.IO
- Message history (when implemented on server)
- Typing indicators (planned)
- Online/offline status
- Message timestamps

### Chat Interface

- Teams-like design with message bubbles
- User avatars and names
- Send button with icon
- Keyboard handling
- Message grouping by sender

## 🔄 State Management

### Authentication State

- User login/logout
- JWT token management
- Secure storage using Expo SecureStore
- Auto-login on app restart

### Socket State

- Connection status monitoring
- Automatic reconnection
- Real-time event handling
- Call session management

## 🚨 Error Handling

- Network connectivity checks
- Authentication error handling
- Call failure notifications
- User-friendly error messages
- Graceful degradation

## 🧪 Testing

### Manual Testing Checklist

- [ ] Login with doctor credentials
- [ ] Login with patient credentials
- [ ] View patient/doctor lists
- [ ] Send chat messages
- [ ] Initiate video calls
- [ ] Initiate audio calls
- [ ] Accept/reject incoming calls
- [ ] Test call controls (mute, video toggle)
- [ ] Test profile screen
- [ ] Test logout functionality

### Automated Testing

_To be implemented_

## 🚀 Deployment

### Building for Production

1. **iOS Build**

   ```bash
   expo build:ios
   ```

2. **Android Build**

   ```bash
   expo build:android
   ```

3. **App Store Deployment**
   - Follow Expo's deployment guide
   - Configure app icons and splash screens
   - Set up push notifications (if needed)

## 🔮 Future Enhancements

- [ ] Push notifications for incoming calls
- [ ] Message history persistence
- [ ] File sharing in chat
- [ ] Group calls (multiple participants)
- [ ] Screen sharing
- [ ] Call recording
- [ ] Offline message queue
- [ ] Dark mode support
- [ ] Accessibility improvements
- [ ] Performance optimizations

## 🐛 Known Issues

- WebRTC may not work on some Android emulators
- Camera permissions required for video calls
- Network connectivity required for all features

## 📞 Support

For technical support or questions:

- Check server logs for API issues
- Verify network connectivity
- Ensure proper JWT token format
- Check SignalR hub connection

## 📄 License

This project is part of the Mental Health Application suite.

---

**Note**: This mobile app is designed to work with the Mental Health Server. Ensure the server is running and properly configured before using the mobile app.
