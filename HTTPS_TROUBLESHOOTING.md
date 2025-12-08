# HTTPS Troubleshooting Guide

## Issues Fixed

### 1. Port Mismatch ✅
- **Problem**: Server `appsettings.json` had HTTPS on port 5263, but `launchSettings.json` and client expected port 5262
- **Fix**: Updated `appsettings.json` to use port 5262 for HTTPS

### 2. WebSocket Scheme ✅
- **Problem**: `WebSocketService` was hardcoded to use `ws://` even when client was on HTTPS
- **Fix**: Now automatically uses `wss://` when HTTPS is detected

## How to Run with HTTPS

### Server
```bash
# Run server with HTTPS profile
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

The server will start on:
- HTTP: `http://0.0.0.0:5262`
- HTTPS: `https://0.0.0.0:5262`

### Client
```bash
# Run client with HTTPS profile
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```

The client will start on:
- HTTPS: `https://0.0.0.0:5282`
- HTTP: `http://0.0.0.0:5283` (fallback)

## SSL Certificate

The server uses a self-signed certificate located at:
- Certificate: `/Users/mohammedkhan/iq/certs/192.168.86.25+2.pem`
- Private Key: `/Users/mohammedkhan/iq/certs/192.168.86.25+2-key.pem`

### Accepting the Certificate in Browser

When you first access `https://192.168.86.25:5262/swagger/index.html`, your browser will show a security warning because it's a self-signed certificate.

**Chrome/Edge:**
1. Click "Advanced"
2. Click "Proceed to 192.168.86.25 (unsafe)"
3. The certificate will be trusted for this session

**Firefox:**
1. Click "Advanced"
2. Click "Accept the Risk and Continue"
3. The certificate will be trusted for this session

**Safari:**
1. Click "Show Details"
2. Click "visit this website"
3. The certificate will be trusted for this session

### Important Notes

1. **Self-Signed Certificates**: These are for development only. In production, use a valid SSL certificate (Let's Encrypt, etc.)

2. **Certificate Trust**: After accepting the certificate in your browser, the client should be able to connect. However, if you're still getting errors:
   - Make sure you've accepted the certificate in the browser first
   - Try accessing `https://192.168.86.25:5262/api/health` directly in the browser
   - Check browser console for any SSL errors

3. **Client Auto-Detection**: The client automatically detects if it's running on HTTPS and connects to the server using HTTPS on the same port (5262).

## Troubleshooting

### Error: ERR_SSL_PROTOCOL_ERROR

This usually means:
1. The server isn't running on HTTPS
2. The certificate path is incorrect
3. The certificate is invalid

**Solution:**
1. Verify the server is running: `curl -k https://192.168.86.25:5262/api/health`
2. Check certificate exists: `ls -la /Users/mohammedkhan/iq/certs/`
3. Verify certificate is valid: `openssl x509 -in /Users/mohammedkhan/iq/certs/192.168.86.25+2.pem -text -noout`

### Error: Cannot Login

If login fails when using HTTPS:
1. Check browser console for errors
2. Verify the server is accessible: `https://192.168.86.25:5262/api/health`
3. Make sure you've accepted the certificate in the browser
4. Check that both client and server are using HTTPS

### WebSocket Connection Fails

The WebSocket service now automatically uses:
- `wss://` when client is on HTTPS
- `ws://` when client is on HTTP

If WebSocket still fails:
1. Check that the server is running
2. Verify the hub URL in browser console
3. Check server logs for connection errors

## Testing

### Test Server HTTPS
```bash
curl -k https://192.168.86.25:5262/api/health
```

### Test Login Endpoint
```bash
curl -k -X POST https://192.168.86.25:5262/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@mentalhealth.com","password":"your-password"}'
```

### Test from Browser
1. Open browser
2. Navigate to: `https://192.168.86.25:5262/swagger/index.html`
3. Accept the certificate warning
4. Try the `/api/health` endpoint in Swagger

## Configuration Files

- **Server HTTPS Config**: `SM_MentalHealthApp.Server/appsettings.json` (Kestrel section)
- **Server Launch Profile**: `SM_MentalHealthApp.Server/Properties/launchSettings.json`
- **Client Launch Profile**: `SM_MentalHealthApp.Client/Properties/launchSettings.json`
- **WebSocket Service**: `SM_MentalHealthApp.Client/Services/WebSocketService.cs`

