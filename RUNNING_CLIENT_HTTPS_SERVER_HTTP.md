# Running Client with HTTPS and Server with HTTP

## ✅ Correct Setup

- **Client**: HTTPS (required for Agora SDK)
- **Server**: HTTP (simpler, no certificate issues)
- **Mixed Content**: Allowed via Content Security Policy (CSP)

## 🚀 How to Run

### Step 1: Start Server (HTTP)

```bash
cd SM_MentalHealthApp.Server
dotnet run
```

**Server will run on:**
- `http://localhost:5262`
- `http://192.168.86.25:5262` (or your IP)

### Step 2: Start Client (HTTPS)

```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```

**Client will run on:**
- `https://localhost:5282`
- `https://192.168.86.25:5282` (or your IP)

### Step 3: Access Client

- **Machine 1**: `https://localhost:5282`
- **Machine 2**: `https://192.168.86.25:5282` (or your IP)

**First time**: Accept the self-signed certificate warning (safe for development)

## 🔧 How It Works

1. **Client runs on HTTPS** → Agora SDK works ✅
2. **Server runs on HTTP** → Simple, no certificate issues ✅
3. **Client calls Server via HTTP** → Allowed by CSP ✅
4. **Browser shows mixed content warnings** → Harmless, requests still work ✅

## 📝 What You'll See

### Browser Console (Normal)
You may see warnings like:
```
Mixed Content: The page at 'https://...' was loaded over HTTPS, 
but requested an insecure resource 'http://...'
```

**These are harmless** - the requests will still work because of the CSP we added.

### Server Console
```
Now listening on: http://0.0.0.0:5262
```

### Client Console
```
🌐 HttpClient BaseAddress configured: http://192.168.86.25:5262/
ℹ️ Note: If you see mixed content errors, run server with HTTPS: dotnet run --launch-profile https
```

## ✅ Verification

1. **Server is running**: Check `http://localhost:5262/api/health` (or any endpoint)
2. **Client is running**: Check `https://localhost:5282`
3. **Login works**: Try logging in from the client
4. **Calls work**: Try making an Agora call

## 🎯 Why This Setup?

- **Agora SDK requires HTTPS** (or localhost) for security
- **HTTP server is simpler** for development (no certificate issues)
- **CSP allows mixed content** so HTTPS client can call HTTP server
- **Production**: Both should use HTTPS, but for dev this works fine

## 🔄 Alternative: Both HTTPS

If you want to eliminate the mixed content warnings:

1. **Start server with HTTPS:**
   ```bash
   cd SM_MentalHealthApp.Server
   dotnet run --launch-profile https
   ```

2. **Update client** `DependencyInjection.cs` line 51:
   ```csharp
   var serverUrl = isLocalhost 
       ? $"http://{baseUri.Host}:5262/" 
       : $"https://{baseUri.Host}:5262/"; // Use HTTPS
   ```

But for development, **HTTP server + HTTPS client is perfectly fine** and simpler!

