# Agora Setup Without Certificates (Testing)

## ✅ Changes Made

I've updated the code to support **testing without certificates**. Here's what changed:

### 1. **Configuration** (`appsettings.json`)
```json
"Agora": {
  "AppId": "b480142a879c4ed2ab7efb07d318abda",
  "UseTokens": false,
  "AppCertificate": ""
}
```

- `UseTokens: false` - Disables token generation
- `AppCertificate: ""` - Empty (not needed for testing)

### 2. **Server Changes**
- `RealtimeController.cs`: Returns empty token when `UseTokens: false`
- `AgoraTokenService.cs`: Reads from configuration (no hardcoded values)

### 3. **Client Changes**
- `AgoraService.cs`: Handles empty tokens gracefully
- `AudioCall.razor` & `VideoCall.razor`: Allow empty tokens
- `index.html`: Passes `null` to Agora SDK when token is empty

## 🧪 How It Works

1. **Server**: When `UseTokens: false`, returns empty token string
2. **Client**: Detects empty token and passes `null` to Agora SDK
3. **Agora SDK**: Accepts `null` token when Token Authentication is disabled in Agora Console

## ⚙️ Configuration

### To Enable Tokens (Production)
```json
"Agora": {
  "AppId": "your-app-id",
  "UseTokens": true,
  "AppCertificate": "your-certificate"
}
```

### To Disable Tokens (Testing)
```json
"Agora": {
  "AppId": "your-app-id",
  "UseTokens": false,
  "AppCertificate": ""
}
```

## 🎯 Important Notes

1. **Agora Console**: Make sure **Token Authentication is DISABLED** in your Agora project
2. **App ID**: Update `AppId` in `appsettings.json` with your new project's App ID
3. **No Certificate Needed**: When `UseTokens: false`, certificate is not used

## ✅ Testing

1. **Update App ID** in `appsettings.json` with your new project's App ID
2. **Restart server**: `dotnet run` in `SM_MentalHealthApp.Server`
3. **Restart client**: `dotnet run --launch-profile https` in `SM_MentalHealthApp.Client`
4. **Try a call**: Should work without tokens!

## 🔒 Security Note

**For production**, you should:
- Enable Token Authentication in Agora Console
- Set `UseTokens: true` in `appsettings.json`
- Provide the `AppCertificate`

But for **testing**, the current setup (no tokens) is fine!

