# How to Run Server with HTTPS

## 🚨 Current Issue

The client is trying to connect to `https://192.168.86.25:5262/` but the server is likely running on HTTP.

## ✅ Solution: Run Server with HTTPS

### Step 1: Start Server with HTTPS Profile

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

This will start the server on:
- **HTTPS**: `https://0.0.0.0:5262` (accessible via `https://192.168.86.25:5262`)
- **HTTP**: `http://0.0.0.0:5262` (accessible via `http://192.168.86.25:5262`)

### Step 2: Accept Self-Signed Certificate

When you first access the server via HTTPS, your browser will show a security warning because it's using a self-signed certificate. This is normal for development.

**Chrome/Edge:**
1. Click "Advanced"
2. Click "Proceed to 192.168.86.25 (unsafe)"

**Firefox:**
1. Click "Advanced"
2. Click "Accept the Risk and Continue"

### Step 3: Update Client to Use HTTPS

I've updated the client to use HTTP by default to avoid certificate issues. If you want to use HTTPS:

1. **Run server with HTTPS** (as shown above)
2. **Update `DependencyInjection.cs`** to use HTTPS:
   ```csharp
   var serverUrl = isLocalhost 
       ? $"http://{baseUri.Host}:5262/" 
       : $"https://{baseUri.Host}:5262/"; // Use HTTPS for IP addresses
   ```

## 🔄 Alternative: Keep Using HTTP

If you prefer to keep using HTTP (simpler for development):

1. **Run server normally:**
   ```bash
   cd SM_MentalHealthApp.Server
   dotnet run
   ```

2. **The client is already configured to use HTTP** (current setup)

3. **You may see mixed content warnings** in the browser console, but they should be allowed by the CSP we added

## 🎯 Recommended for Development

**Use HTTP for both** (simpler, no certificate issues):
- Server: `dotnet run` (HTTP on port 5262)
- Client: `dotnet run --launch-profile https` (HTTPS on port 5282 for Agora)
- Client will call server via HTTP (allowed by CSP)

**Use HTTPS for both** (production-like):
- Server: `dotnet run --launch-profile https` (HTTPS on port 5262)
- Client: `dotnet run --launch-profile https` (HTTPS on port 5282)
- Client will call server via HTTPS (no mixed content)

## 📝 Current Configuration

The client is currently set to use **HTTP** for the server API to avoid certificate issues. This works fine for development, but you'll see mixed content warnings in the console (they're harmless).

If you want to eliminate the warnings, run the server with HTTPS and update the client code to use HTTPS.

