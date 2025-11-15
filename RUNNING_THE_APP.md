# How to Run the Application

## 🚀 Quick Start

### 1. **Server** (Backend API)
```bash
cd SM_MentalHealthApp.Server
dotnet run
```
- ✅ Runs on: `http://localhost:5262` (or `http://0.0.0.0:5262`)
- ✅ **HTTP is fine** - server doesn't need HTTPS
- ✅ Can be accessed from other machines via IP: `http://mac_ip:5262`

### 2. **Client** (Blazor WebAssembly)
```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```
- ✅ Runs on: `https://localhost:5282` (HTTPS required for Agora)
- ✅ Also available on: `http://localhost:5283` (for non-Agora features)
- ✅ **HTTPS is required** for Agora calls to work

## 📋 Complete Setup

### Terminal 1: Server
```bash
cd /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Server
dotnet run
```
**Output:**
```
Now listening on: http://0.0.0.0:5262
```

### Terminal 2: Client
```bash
cd /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Client
dotnet run --launch-profile https
```
**Output:**
```
Now listening on: https://0.0.0.0:5282
Now listening on: http://0.0.0.0:5283
```

## 🌐 Accessing from Different Machines

### Machine 1 (Same machine as server):
- **Server API**: `http://localhost:5262`
- **Client App**: `https://localhost:5282`

### Machine 2 (Different machine):
- **Server API**: `http://mac_ip:5262` (or `http://YOUR_MAC_IP:5262`)
- **Client App**: `https://mac_ip:5282` (or `https://YOUR_MAC_IP:5282`)

## ⚠️ Important Notes

1. **Server can use HTTP** - it's just an API, doesn't need HTTPS
2. **Client MUST use HTTPS** - Agora SDK requires it for microphone/camera
3. **Certificate Warning** - Accept the self-signed certificate on first access (safe for dev)
4. **Both machines** accessing the client must use HTTPS for calls to work

## 🔍 Why This Setup?

- **Server (HTTP)**: Just serves API endpoints, no browser security restrictions
- **Client (HTTPS)**: Runs in browser, needs HTTPS for Agora SDK (microphone/camera access)

## ✅ Verification

1. Server running: Check `http://localhost:5262/api/health` (or similar endpoint)
2. Client running: Open `https://localhost:5282` in browser
3. Accept certificate warning (first time only)
4. Test a call - should work now! 🎉

