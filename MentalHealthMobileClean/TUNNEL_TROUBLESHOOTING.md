# Expo Tunnel Connection Troubleshooting

## Error: "ngrok tunnel took too long to connect"

This happens when Expo's tunnel service (ngrok) is slow or unavailable.

## Solutions (Try in Order)

### Solution 1: Wait Longer
Sometimes tunnel just needs more time:
```bash
npx expo start --tunnel --clear
# Wait 60-90 seconds for tunnel to establish
```

### Solution 2: Use Reduced Workers
```bash
npx expo start --tunnel --clear --max-workers 1
```

### Solution 3: Use Alternative Tunnel Script
```bash
./start-expo-tunnel-alternative.sh
```

### Solution 4: Manual Tunnel Selection
```bash
npx expo start --clear
# When it starts, press 's' to switch connection mode
# Choose 'tunnel' from the menu
```

### Solution 5: Use Expo Dev Client (More Reliable)
If you have a development build:
```bash
npx expo start --dev-client --tunnel
```

### Solution 6: Check Internet Connection
Tunnel requires stable internet:
```bash
# Test connection
ping -c 3 expo.dev
```

### Solution 7: Clear Expo Cache
```bash
rm -rf .expo
rm -rf node_modules/.cache
npx expo start --tunnel --clear
```

### Solution 8: Use LAN + VPN (Advanced)
If you have VPN access to your home network:
1. Connect to VPN from your phone
2. Use LAN mode: `./start-expo-lan.sh`
3. Phone will connect via VPN as if on same network

## Alternative: Use ngrok Directly

If Expo tunnel keeps failing, use ngrok directly:

1. **Install ngrok:**
   ```bash
   brew install ngrok
   # OR download from https://ngrok.com/download
   ```

2. **Start ngrok tunnel:**
   ```bash
   ngrok http 8081
   ```

3. **Start Expo normally:**
   ```bash
   npx expo start --clear
   ```

4. **Use ngrok URL in Expo Go:**
   - Copy the ngrok URL (e.g., `https://abc123.ngrok.io`)
   - In Expo Go, manually enter: `exp://abc123.ngrok.io:80`

## Why Tunnel Fails

1. **Network issues:** Slow/unstable internet
2. **Firewall:** Blocking tunnel connections
3. **ngrok service:** Expo's ngrok service may be overloaded
4. **Timeout:** Default timeout too short for slow connections

## Best Practices

- **At home:** Always use LAN mode (`--host lan`) - it's faster
- **Remote:** Try tunnel, but have VPN as backup if possible
- **Development:** Consider building a development client for more reliable connections

