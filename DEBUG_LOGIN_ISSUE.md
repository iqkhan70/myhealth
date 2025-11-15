# Debugging Login Issue from Machine 2

## 🔍 Problem
Machine 2 can access the login screen at `https://macip:5282`, but login fails even with correct credentials.

## 🎯 Possible Causes

### 1. **Mixed Content (HTTPS → HTTP)**
When accessing via HTTPS, browsers may block HTTP API calls. However, modern browsers allow this for API calls, but might show warnings.

### 2. **Server Not Accessible**
The server might not be accessible from machine 2 at `http://macip:5262`.

### 3. **CORS Issues**
Even though CORS allows any origin in development, there might be preflight issues.

## ✅ Debugging Steps

### Step 1: Check Browser Console
Open browser DevTools (F12) on machine 2 and check:
- **Console tab**: Look for errors
- **Network tab**: Check if the login request is being made
  - Look for `POST http://macip:5262/api/auth/login`
  - Check the status code (should be 200)
  - Check if there's a CORS error

### Step 2: Test Server Accessibility
From machine 2, try accessing the server directly:
```bash
# In browser or curl
http://macip:5262/api/health
# Or
curl http://macip:5262/api/health
```

### Step 3: Check Server Logs
On the server machine, check the console output when you try to login:
- Is the login request reaching the server?
- Are there any errors in the server logs?

### Step 4: Check Network Tab Details
In browser DevTools → Network tab:
1. Try to login
2. Find the `api/auth/login` request
3. Check:
   - **Request URL**: Should be `http://macip:5262/api/auth/login`
   - **Status Code**: What is it? (200 = success, 401 = unauthorized, 500 = server error, CORS error = blocked)
   - **Response**: What does it say?

## 🔧 Quick Fixes to Try

### Fix 1: Verify Server is Running on 0.0.0.0
Make sure server is bound to `0.0.0.0:5262` (not just `localhost:5262`):
```json
"applicationUrl": "http://0.0.0.0:5262"
```

### Fix 2: Check Firewall
Make sure port 5262 is not blocked by firewall on the server machine.

### Fix 3: Test with curl
From machine 2, test the login endpoint directly:
```bash
curl -X POST http://macip:5262/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email@example.com","password":"your-password"}'
```

## 📝 What to Share
If login still fails, please share:
1. **Browser Console errors** (F12 → Console tab)
2. **Network tab details** for the login request (F12 → Network tab → find `api/auth/login`)
3. **Server console output** when login is attempted
4. **Status code** from the login request

