# How to Verify Agora App ID and Certificate

## 🚨 The Error

```
AgoraRTCError CAN_NOT_GET_GATEWAY_SERVER: invalid vendor key, can not find appid
```

This means the **App ID** or **App Certificate** is incorrect, or there's a mismatch between:
- The App ID used to **initialize** Agora
- The App ID used to **generate the token**
- The App ID used to **join the channel**

## ✅ Step-by-Step Verification

### Step 1: Check Your Agora Console

1. **Go to Agora Console**: https://console.agora.io/
2. **Login** to your account
3. **Select your project** (or create one if you don't have one)
4. **Go to Project Management** → **Project List**
5. **Click on your project** to see details
6. **Copy the App ID** and **App Certificate**

### Step 2: Verify App ID in Code

Check these locations in your codebase:

#### A. Server Configuration
**File**: `SM_MentalHealthApp.Server/Controllers/RealtimeController.cs`
```csharp
private const string _appId = "efa11b3a7d05409ca979fb25a5b489ae";
```

**File**: `SM_MentalHealthApp.Server/Services/AgoraTokenService.cs`
```csharp
private readonly string _appId = "efa11b3a7d05409ca979fb25a5b489ae";
private readonly string _appCertificate = "89ab54068fae46aeaf930ffd493e977b";
```

#### B. Client Hardcoded Values
**File**: `SM_MentalHealthApp.Client/Pages/VideoCall.razor`
```csharp
const string AGORA_APP_ID = "efa11b3a7d05409ca979fb25a5b489ae";
```

**File**: `SM_MentalHealthApp.Client/Pages/AudioCall.razor`
```csharp
await AgoraService.InitializeAsync("efa11b3a7d05409ca979fb25a5b489ae");
```

#### C. Fallback Values
**File**: `SM_MentalHealthApp.Client/Services/AgoraService.cs`
```csharp
appId = "efa11b3a7d05409ca979fb25a5b489ae"; // Fallback
```

### Step 3: Verify They All Match

**All App IDs should be identical!** If any are different, that's the problem.

### Step 4: Test Token Generation

1. **Make a test API call** to verify token generation:
   ```bash
   curl -X GET "http://localhost:5262/api/realtime/token?channel=test_channel&uid=123" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

2. **Check the response**:
   ```json
   {
     "agoraAppId": "efa11b3a7d05409ca979fb25a5b489ae",
     "token": "006efa11b3a7d05409ca979fb25a5b489ae...",
     "cached": false
   }
   ```

3. **Verify**:
   - `agoraAppId` matches your Agora Console App ID
   - `token` starts with `006` (version prefix)
   - `token` contains your App ID after the version

### Step 5: Check Browser Console

When you try to join a call, check the browser console for:

```
🎯 Web App: Joining Agora channel...
App ID: efa11b3a7d05409ca979fb25a5b489ae
Channel: call_2_3
Token: 006efa11b3a7d05409ca979fb25a5b489ae...
UID: 2
```

**Verify**:
- App ID matches your Agora Console
- Token starts with `006`
- Token contains the App ID

## 🔧 How to Fix

### If App ID is Wrong

1. **Update Server**:
   - `SM_MentalHealthApp.Server/Controllers/RealtimeController.cs` (line 26)
   - `SM_MentalHealthApp.Server/Services/AgoraTokenService.cs` (line 11)

2. **Update Client**:
   - `SM_MentalHealthApp.Client/Pages/VideoCall.razor` (line 370)
   - `SM_MentalHealthApp.Client/Pages/AudioCall.razor` (line 287)
   - `SM_MentalHealthApp.Client/Services/AgoraService.cs` (line 300, 82)

3. **Restart both server and client**

### If App Certificate is Wrong

1. **Update Server**:
   - `SM_MentalHealthApp.Server/Services/AgoraTokenService.cs` (line 12)

2. **Restart server**

### If There's a Mismatch

The most common issue is:
- **Initialization** uses one App ID
- **Token generation** uses a different App ID
- **Joining** uses yet another App ID

**Solution**: Make sure all three use the **same App ID** from your Agora Console.

## 🧪 Quick Test Script

Create a test file `test-agora-config.sh`:

```bash
#!/bin/bash

APP_ID="efa11b3a7d05409ca979fb25a5b489ae"
CERT="89ab54068fae46aeaf930ffd493e977b"

echo "🔍 Checking Agora Configuration..."
echo ""

echo "📋 Server Files:"
grep -r "$APP_ID" SM_MentalHealthApp.Server/ --include="*.cs" | wc -l
echo "   Found App ID in server files"

echo ""
echo "📋 Client Files:"
grep -r "$APP_ID" SM_MentalHealthApp.Client/ --include="*.razor" --include="*.cs" | wc -l
echo "   Found App ID in client files"

echo ""
echo "✅ If counts match, all files use the same App ID"
echo "❌ If counts differ, there's a mismatch"
```

## 📝 Common Issues

1. **App ID from different project**: Make sure you're using the App ID from the correct Agora project
2. **App Certificate mismatch**: App Certificate must match the App ID
3. **Token generated with wrong App ID**: Token must be generated with the same App ID used to initialize
4. **Expired or invalid certificate**: Check if your App Certificate is still valid in Agora Console

## 🎯 Next Steps

1. **Verify App ID in Agora Console**
2. **Update all hardcoded values** to match
3. **Test token generation** with the API
4. **Check browser console** when joining a call
5. **Share the results** if still having issues

