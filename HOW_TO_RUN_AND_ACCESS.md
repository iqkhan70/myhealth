# How to Run and Access the Application

## Architecture Overview

- **Server (API)**: Runs on port **5262** (HTTP or HTTPS)
- **Client (Blazor Web App)**: Runs on port **5282** (HTTP or HTTPS)
- **Mobile App**: Connects directly to server on port **5262**

## Running the Application

### Option 1: HTTPS Mode (Recommended for Agora)

**Terminal 1 - Server:**
```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```
Server runs on: `https://192.168.86.25:5262`

**Terminal 2 - Client:**
```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```
Client runs on: `https://192.168.86.25:5282`

**Access the app:**
- Open browser: `https://192.168.86.25:5282`
- Accept the SSL certificate warning (self-signed cert)
- Login and use the app

### Option 2: HTTP Mode (Easier for Development)

**Terminal 1 - Server:**
```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile http
```
Server runs on: `http://192.168.86.25:5262`

**Terminal 2 - Client:**
```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile http
```
Client runs on: `http://192.168.86.25:5282`

**Access the app:**
- Open browser: `http://192.168.86.25:5282`
- No certificate issues!
- Login and use the app

**Note:** Agora video/audio calls won't work in HTTP mode (requires HTTPS)

### Option 3: Both HTTP and HTTPS (Server Only)

**Terminal 1 - Server (Both):**
```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile both
```
Server runs on:
- HTTP: `http://192.168.86.25:5262`
- HTTPS: `https://192.168.86.25:5263`

**Terminal 2 - Client (Choose one):**
```bash
# For HTTP client
cd SM_MentalHealthApp.Client
dotnet run --launch-profile http

# OR for HTTPS client
dotnet run --launch-profile https
```

## Important Notes

1. **Client-Server Matching**: 
   - If client runs on **HTTP**, it connects to server on **HTTP** (port 5262)
   - If client runs on **HTTPS**, it connects to server on **HTTPS** (port 5262)

2. **SSL Certificate Warning**:
   - When accessing via HTTPS, your browser will show a certificate warning
   - Click "Advanced" → "Proceed to 192.168.86.25 (unsafe)"
   - This is normal for self-signed certificates in development

3. **Agora Requirement**:
   - Agora video/audio calls **require HTTPS**
   - If you need calls to work, use HTTPS mode
   - If you're just testing other features, HTTP mode is fine

4. **Mobile App**:
   - Mobile app connects directly to server (port 5262)
   - Configure in `MentalHealthMobileClean/src/config/app.config.js`:
     - `USE_HTTPS: true` for HTTPS
     - `USE_HTTPS: false` for HTTP
     - `SERVER_PORT: 5262`

## Troubleshooting

### "Cannot connect to server" Error

**If using HTTPS:**
1. First, access the server directly in browser: `https://192.168.86.25:5262/api/health`
2. Accept the certificate warning
3. Then access the client: `https://192.168.86.25:5282`

**If using HTTP:**
- Make sure both server and client are running in HTTP mode
- Check that server is on port 5262
- Check that client is on port 5282

### Port Already in Use

If you get "port already in use" error:
```bash
# Find what's using the port
lsof -i :5262
lsof -i :5282

# Kill the process
kill -9 <PID>
```

## Quick Reference

| Mode | Server URL | Client URL | Access URL |
|------|-----------|------------|------------|
| HTTPS | `https://192.168.86.25:5262` | `https://192.168.86.25:5282` | `https://192.168.86.25:5282` |
| HTTP | `http://192.168.86.25:5262` | `http://192.168.86.25:5282` | `http://192.168.86.25:5282` |
| Both | `http://192.168.86.25:5262`<br>`https://192.168.86.25:5263` | `http://192.168.86.25:5282` or `https://192.168.86.25:5282` | Match client URL |

