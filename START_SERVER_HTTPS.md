# Start Server on HTTPS

## ✅ Port is Free

Port 5262 is now free. You can start the server.

## 🚀 Start Server

```bash
cd SM_MentalHealthApp.Server
dotnet run --launch-profile https
```

## 📋 What You Should See

```
Now listening on: https://0.0.0.0:5262
Now listening on: http://0.0.0.0:5262
Application started. Press Ctrl+C to shut down.
```

## ✅ Verify It's Running

From another terminal or machine:
```bash
# Test HTTPS endpoint
curl -k https://192.168.86.25:5262/api/health

# Should return: {"status":"healthy","timestamp":"..."}
```

The `-k` flag skips certificate verification (for self-signed certs).

## 🔧 If Port Still in Use

If you get "address already in use" error:

```bash
# Kill whatever is on port 5262
kill -9 $(lsof -ti :5262) 2>/dev/null

# Or use the script:
./kill-server.sh
```

Then try starting again.

## 🎯 Next Steps

1. **Start server**: `dotnet run --launch-profile https`
2. **Start client**: `dotnet run --launch-profile https` (in Client directory)
3. **Access**: `https://192.168.86.25:5282`
4. **Accept certificate**: Click through the warning
5. **Login**: Should work now! ✅

