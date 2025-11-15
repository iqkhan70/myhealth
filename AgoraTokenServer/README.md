# Agora Token Server (Node.js)

This is a standalone Node.js server for generating Agora tokens. **Note: This is currently NOT being used** - the .NET server handles token generation instead.

## Prerequisites

- Node.js (v14 or higher)
- Redis server running on localhost:6379 with password: `StrongPassword123!`

## Installation

```bash
cd AgoraTokenServer
npm install
```

## Running the Server

```bash
npm start
```

Or directly:
```bash
node server.js
```

The server will start on port 3000 (or the PORT environment variable if set).

## Endpoints

- `GET /realtime/token?channel={channelName}&uid={uid}` - Generate Agora token
- `GET /health` - Health check (checks Redis connection)

## Configuration

The server uses:
- **Redis**: localhost:6379 (password: `StrongPassword123!`)
- **Agora App ID**: `efa11b3a7d05409ca979fb25a5b489ae`
- **Agora App Certificate**: `89ab54068fae46aeaf930ffd493e977b`

## To Use This Instead of .NET Server

If you want to use this Node.js server instead of the .NET token generation:

1. Update `MentalHealthMobileClean/App.js` to point to this server:
   ```javascript
   const url = `http://localhost:3000/realtime/token?channel=${encodeURIComponent(channelName)}&uid=${uid}`;
   ```

2. Update `SM_MentalHealthApp.Client/Services/AgoraService.cs` to point to this server:
   ```csharp
   var url = $"http://localhost:3000/realtime/token?channel={Uri.EscapeDataString(channelName)}&uid={uid}";
   ```

## Note

Currently, all clients are using the .NET server endpoint (`api/realtime/token`), so this Node.js server is redundant and can be safely removed if not needed.

