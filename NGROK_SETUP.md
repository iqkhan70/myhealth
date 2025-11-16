# Ngrok Setup Guide

This guide explains how to use ngrok to expose your Blazor client and .NET server for external access.

## Prerequisites

- ngrok installed (`brew install ngrok` or download from https://ngrok.com)
- Client running on HTTPS port 5282
- Server running on HTTPS port 5262

## Quick Start

### Option 1: Use Separate Scripts (Recommended)

1. **Start the server:**

   ```bash
   cd SM_MentalHealthApp.Server
   dotnet run --launch-profile https
   ```

2. **Start the client:**

   ```bash
   cd SM_MentalHealthApp.Client
   dotnet run --launch-profile https
   ```

3. **In a new terminal, start ngrok for the server:**

   ```bash
   ./start-ngrok-server.sh
   ```

   Note the ngrok URL (e.g., `https://abc123.ngrok.io`)

4. **In another new terminal, start ngrok for the client:**

   ```bash
   ./start-ngrok-client.sh
   ```

   Note the ngrok URL (e.g., `https://xyz789.ngrok.io`)

5. **Access the client with the server URL:**

   - Open the client ngrok URL in your browser with a query parameter:
   - `https://xyz789.ngrok.io?server=https://abc123.ngrok.io`
   - Replace `abc123.ngrok.io` with your server ngrok URL from step 3
   - The server URL will be saved to localStorage for next time

   **OR** set the environment variable before starting:

   ```bash
   export SERVER_NGROK_URL=https://your-server-ngrok-url.ngrok.io
   cd SM_MentalHealthApp.Client
   dotnet run --launch-profile https
   ```

### Option 2: Use Both Tunnels Script (macOS only)

```bash
./start-ngrok-both.sh
```

This will open both ngrok tunnels in separate terminal windows.

## Manual Configuration

If you need to manually configure the server URL when using ngrok:

1. **Get your server ngrok URL** (e.g., `https://abc123.ngrok.io`)

2. **Update `DependencyInjection.cs`:**

   ```csharp
   // In AddHttpClient method, replace the serverUrl logic with:
   var serverUrl = "https://abc123.ngrok.io/"; // Your server ngrok URL
   ```

3. **Rebuild and restart the client**

## Important Notes

### Agora Requirements

- Agora SDK requires HTTPS or localhost
- ngrok provides HTTPS, so it works perfectly with Agora
- Make sure both client and server are accessible via HTTPS

### CORS Configuration

- The server must allow requests from the client ngrok URL
- Check `Program.cs` in the server for CORS configuration
- You may need to add your ngrok URL to allowed origins

### SignalR/WebSocket

- SignalR connections should work automatically through ngrok
- The client constructs the hub URL from the HttpClient BaseAddress

### Testing

1. Access the client via the client ngrok URL
2. The client will automatically use the server ngrok URL for API calls
3. All features (calls, chat, etc.) should work as expected

## Troubleshooting

### "Cannot connect to server" when accessing from another machine

**If using ngrok (RECOMMENDED):**

1. Make sure both client and server ngrok tunnels are running
2. Access the client with the `?server=` parameter:
   ```
   https://your-client-ngrok-url.ngrok.io?server=https://your-server-ngrok-url.ngrok.io
   ```
3. Check browser console for server URL configuration messages
4. The server URL will be saved to localStorage for next time

**If NOT using ngrok (direct IP access - may have issues):**

1. Make sure the server is running on HTTPS port 5262
2. Check firewall allows port 5262 on the server machine
3. Verify both machines are on the same network
4. Try accessing the server directly in browser: `https://192.168.86.25:5262/api/health`
5. **Recommended:** Use ngrok instead for better reliability and HTTPS support

### "Cannot connect to server" (general)

- Make sure the server ngrok tunnel is running (if using ngrok)
- Check that the server is running on port 5262
- Verify the server ngrok URL is correct
- Check browser console for detailed error messages

### "Mixed content" errors

- Both client and server should use HTTPS
- ngrok provides HTTPS automatically
- Make sure you're accessing the client via the ngrok HTTPS URL

### Agora connection issues

- Agora works with ngrok HTTPS URLs
- Make sure you're using the HTTPS ngrok URL (not HTTP)
- Check browser console for Agora errors

## Stopping Ngrok

Press `Ctrl+C` in the terminal where ngrok is running, or close the terminal window.
