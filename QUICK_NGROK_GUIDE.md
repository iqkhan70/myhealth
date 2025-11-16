# Quick Ngrok Guide for Remote Access

## Problem
When accessing the client from another machine, you get:
```
Cannot connect to server at https://192.168.86.25:5262/
```

## Solution: Use Ngrok

### Step 1: Start Server Ngrok Tunnel

On the server machine, in a terminal:
```bash
./start-ngrok-server.sh
```

Copy the ngrok URL (e.g., `https://abc123.ngrok.io`)

### Step 2: Start Client Ngrok Tunnel

On the server machine, in another terminal:
```bash
./start-ngrok-client.sh
```

Copy the ngrok URL (e.g., `https://xyz789.ngrok.io`)

### Step 3: Access Client with Server URL

From the other machine, open in browser:
```
https://xyz789.ngrok.io?server=https://abc123.ngrok.io
```

**Important:** Include the `?server=` parameter with your server ngrok URL!

### Step 4: Done!

- The server URL will be saved to localStorage
- Next time, you can just use: `https://xyz789.ngrok.io` (it will remember the server URL)

## Why Ngrok?

- ✅ Works from any network (not just same LAN)
- ✅ Provides HTTPS automatically (required for Agora)
- ✅ No firewall configuration needed
- ✅ More reliable than direct IP access

## Troubleshooting

**Still can't connect?**
1. Check browser console (F12) for error messages
2. Make sure both ngrok tunnels are running
3. Verify you included the `?server=` parameter
4. Check that server is running: `dotnet run --launch-profile https` in `SM_MentalHealthApp.Server`

