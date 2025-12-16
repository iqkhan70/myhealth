# Expo Go Remote Connection Guide

## The Problem

When you're at **home**, your phone connects fine to Expo Go because both your Mac and phone are on the same WiFi network.

When you're at a **different place**, your phone can't connect because:
- Your phone is on a different WiFi network (or cellular)
- It can't reach your Mac's local IP address (e.g., `192.168.86.34`)
- The Mac's local IP is only accessible on the same network

## Solutions

### Option 1: Use Tunnel Mode (Works from Anywhere) ⭐ RECOMMENDED

**When to use:** When you're at a different location than your Mac

```bash
cd MentalHealthMobileClean
./start-expo-tunnel.sh
```

OR:
```bash
npm run start:tunnel
```

**How it works:**
- Expo creates a tunnel through their servers
- Your phone connects via a public URL (e.g., `exp://u.expo.dev/...`)
- Works from anywhere in the world
- **Note:** May be slower than LAN mode

### Option 2: Use LAN Mode (Same WiFi Only)

**When to use:** When you're at home or on the same WiFi as your Mac

```bash
cd MentalHealthMobileClean
./start-expo-lan.sh
```

OR:
```bash
npm run start:lan
```

**How it works:**
- Uses your Mac's local IP address
- Phone must be on the same WiFi network
- Faster than tunnel mode
- **Won't work** if phone is on different network

### Option 3: Manual Selection

When you run `npx expo start`, Expo will show you options:
- Press `s` to switch connection mode
- Choose `tunnel` for remote access
- Choose `lan` for same WiFi

## Quick Reference

| Location | Use This Command |
|----------|----------------|
| **At home** (same WiFi) | `./start-expo-lan.sh` or `npm run start:lan` |
| **Different place** (remote) | `./start-expo-tunnel.sh` or `npm run start:tunnel` |
| **Not sure** | `npx expo start` then press `s` to switch modes |

## Troubleshooting

### Tunnel mode is slow?
- This is normal - tunnel goes through Expo's servers
- Use LAN mode when on same WiFi for better performance

### Tunnel mode won't start?
- Check your internet connection
- Make sure port 8081 isn't blocked by firewall
- Try: `npx expo start --tunnel --clear`

### Still can't connect?
1. Make sure Metro bundler is running
2. Check the QR code/URL shown in terminal
3. Scan QR code with Expo Go app
4. Or manually enter the URL in Expo Go

## Summary

- **Home/Same WiFi:** Use `--host lan` (faster)
- **Different Location:** Use `--tunnel` (works from anywhere, slower)

