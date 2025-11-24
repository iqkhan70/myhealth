# Debugging Logs Guide

## Where to Check Logs

### 1. **Metro Bundler Console** (Terminal where you ran `npx react-native start`)
- This is where you see logs like:
  - `📞 Contacts response status: 200`
  - `✅ Contacts updated with 2 contacts`
  - `🔌 SignalR: Connected successfully`
  - `📨 SignalR: New message received:`

### 2. **React Native Debugger** (if enabled)
- Press `Cmd+D` (iOS) or `Cmd+M` (Android) on simulator/device
- Select "Debug" or "Open Debugger"
- Opens Chrome DevTools
- Check Console tab for logs

### 3. **Xcode Console** (iOS Simulator/Device)
- If running on iOS Simulator, check Xcode console
- Shows native logs and React Native logs

### 4. **Android Logcat** (Android)
- Run: `adb logcat | grep ReactNativeJS`
- Shows React Native JavaScript logs

## What to Look For

### When App Starts:
1. `✅ API Base URL initialized:`
2. `✅ SignalR Hub URL:`
3. `🔌 App: Initializing SignalR connection to:`
4. `✅ SignalR: Connected successfully!`
5. `✅ SignalR: Connection ID:`
6. `📱 App: ========== USER REF UPDATED ==========`
7. `📱 App: userRef updated - prev: undefined new: 3`
8. `📱 App: Setting up SignalR message listener. Current userRef: 3`

### When You Send a Message from Computer:
1. **Server Console** should show:
   - `📨 Sending SignalR notification to user...`
   - `✅ SignalR notification sent successfully...`

2. **Metro Bundler Console** should show:
   - `📨 SignalR: New message received:`
   - `📱 App: ========== MESSAGE RECEIVED ==========`
   - `📱 App: userRef.current at message time:`
   - `📱 App: ✅ Current user ID from ref: 3`
   - `📱 App: ✅ Adding new message to chat:`

## Troubleshooting

### If you don't see any logs:
1. **Check Metro Bundler is running**: Look for the terminal where you ran `npx react-native start`
2. **Reload the app**: Shake device → "Reload" or press `Cmd+R` (iOS) / `R+R` (Android)
3. **Check if logs are filtered**: Make sure console.log isn't being filtered out

### If SignalR isn't connecting:
- Look for `❌ SignalR: Connection error:` in Metro console
- Check network connectivity
- Verify server is running and accessible

### If messages aren't appearing:
- Check if `userRef.current` is null when message arrives
- Look for `📱 App: ❌ No current user, ignoring message`
- Verify you're in chat view: `📱 App: Not in chat view`

## Quick Test

1. **Reload app** (shake device → Reload)
2. **Log in**
3. **Check Metro console** for:
   - `📱 App: ========== USER REF UPDATED ==========`
   - `📱 App: userRef updated - prev: undefined new: 3` (your user ID)
   - `📱 App: Setting up SignalR message listener. Current userRef: 3`
4. **Open a chat** with a contact
5. **Send message from computer**
6. **Check Metro console** for:
   - `📱 App: ========== MESSAGE RECEIVED ==========`
   - `📱 App: ✅ Adding new message to chat:`

If you see "MESSAGE RECEIVED" but not "Adding new message", check the logs to see why it's being filtered out.

