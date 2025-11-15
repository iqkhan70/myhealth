# HTTPS Setup Guide for Agora Calls

## 🚨 The Problem

**Agora SDK requires HTTPS or localhost** for security reasons (microphone/camera access).

When you access via:
- ✅ `http://localhost:5282` - **WORKS** (localhost exception)
- ❌ `http://mac_ip:5282` - **FAILS** (not localhost, not HTTPS)

## ✅ Solution: Use HTTPS

You have two options:

### Option 1: Use HTTPS Profile (Recommended)

1. **Update Client Launch Settings** to include HTTPS:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://0.0.0.0:5282;http://0.0.0.0:5283",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

2. **Run with HTTPS profile:**
```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```

3. **Access via:**
   - Machine 1: `https://localhost:5282`
   - Machine 2: `https://mac_ip:5282`
   - **Accept the self-signed certificate warning** (it's safe for development)

### Option 2: Use the HTTPS Server Script

You already have `simple-https-server.js` in the Client folder. This creates an HTTPS server.

1. **Generate SSL certificates** (if not already done):
```bash
cd SM_MentalHealthApp.Client
openssl req -x509 -newkey rsa:2048 -keyout webapp-key.pem -out webapp-cert.pem -days 365 -nodes
```

2. **Run the HTTPS server:**
```bash
node simple-https-server.js
```

3. **Access via:**
   - Machine 1: `https://localhost:5443` (or whatever port the script uses)
   - Machine 2: `https://mac_ip:5443`

## 🔧 Quick Fix: Update Launch Settings

Let me update your client launch settings to support HTTPS:

