# Emergency Mock App

A React Native mock application that simulates emergency messages from smart devices to the Mental Health App emergency system. **Currently using mock responses for testing - no server connection required.**

## Features

- **Device Registration**: Register your device with the emergency system (mock)
- **Emergency Simulation**: Test different emergency scenarios (mock)
- **Real-time Location**: Uses device location for emergency messages
- **Vital Signs Simulation**: Generates realistic vital signs data

## Setup

1. Install dependencies:

   ```bash
   npm install
   ```

2. Start the development server:

   ```bash
   npm start
   ```

3. Run on your device:
   - Install Expo Go app on your phone
   - Scan the QR code from the terminal
   - Or run `npm run ios` / `npm run android` for simulators

## Usage

### 1. Register Device

- **Enter your Patient ID** - Use any patient ID (1, 7, or 9 work well)
- The app will generate a unique device ID
- Tap "Register Device" to register (uses mock response - no server connection needed)

### 2. Test Emergencies

Once registered, you can test different emergency scenarios:

- **Fall** - Critical severity
- **Cardiac** - Critical severity
- **Panic Attack** - High severity
- **Seizure** - Critical severity
- **Overdose** - Critical severity
- **Self Harm** - High severity

### 3. Emergency Flow

When you tap an emergency button:

1. App generates realistic vital signs data
2. Simulates sending emergency message (mock response)
3. Shows success message with incident ID

## Current Status

- ✅ **Device Registration**: Working with mock responses
- ✅ **Emergency Simulation**: Working with mock responses
- 🔄 **Server Integration**: Temporarily disabled due to authorization issues
- 🔄 **Real-time Notifications**: Pending server integration

## Next Steps

Once the server authorization issues are resolved, the app will be updated to:

1. Connect to the actual server endpoints
2. Send real emergency data to the backend
3. Receive real-time notifications from doctors
4. Implement proper device token validation
5. Add rate limiting and security features
