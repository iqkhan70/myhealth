# Fix: Unable to Start Server on HTTPS

## 🔍 Common Issues

### Issue 1: Certificate Not Trusted

**Solution:**
```bash
cd SM_MentalHealthApp.Server
dotnet dev-certs https --trust
```

This will:
- Trust the development certificate
- Allow HTTPS to work without browser warnings
- May require your password

### Issue 2: Port Already in Use

**Check if port 5262 is in use:**
```bash
lsof -i :5262
```

**If something is using it:**
- Stop the other process
- Or change the port in `launchSettings.json`

### Issue 3: Certificate Issues

**Regenerate certificate:**
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

## ✅ How to Start Server with HTTPS

### Step 1: Trust the Certificate
```bash
cd SM_MentalHealthApp.Server
dotnet dev-certs https --trust
```

### Step 2: Start Server
```bash
dotnet run --launch-profile https
```

### Step 3: Verify It's Running
You should see:
```
Now listening on: https://0.0.0.0:5262
Now listening on: http://0.0.0.0:5262
```

## 🚨 If Still Not Working

### Check the Error Message

**If you see "Failed to bind to address":**
- Port is already in use
- Kill the process: `kill -9 $(lsof -t -i:5262)`

**If you see "Certificate error":**
- Run: `dotnet dev-certs https --clean --trust`

**If you see "Permission denied":**
- Make sure you're not using a restricted port
- Try a different port (e.g., 5001)

### Alternative: Use Different Port

Update `launchSettings.json`:
```json
"https": {
  "applicationUrl": "https://0.0.0.0:5001;http://0.0.0.0:5262"
}
```

Then update client `DependencyInjection.cs` to use port 5001 for HTTPS.

## 📝 Quick Test

1. **Trust certificate:**
   ```bash
   dotnet dev-certs https --trust
   ```

2. **Start server:**
   ```bash
   dotnet run --launch-profile https
   ```

3. **Test in browser:**
   - Open: `https://localhost:5262/api/health` (or any endpoint)
   - Should work without certificate errors

## 🎯 Recommended: Keep Using HTTP

For development, **HTTP is simpler**:
- No certificate issues
- No browser warnings
- Works with the CSP we added

**Just run:**
```bash
dotnet run
```

The client will call the server via HTTP (allowed by CSP), and you'll only see harmless console warnings.

