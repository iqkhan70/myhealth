# Fixing .NET Runtime Crash on Login

## 🚨 The Problem

When accessing from machine 2 via `https://macip:5282` and trying to login, the .NET runtime crashes with:
```
Assert failed: .NET runtime already exited with 1
Error: Assert failed: ERR18: expected ws instance
```

## 🔍 Root Cause

This typically happens when:
1. **Unhandled exception** crashes the runtime
2. **Network error** (can't reach server) causes fatal error
3. **HttpClient failure** not properly caught

## ✅ Fixes Applied

### 1. **Better Error Handling in Program.cs**
- Added try-catch around host startup
- Added logging configuration
- Prevents runtime from crashing on startup errors

### 2. **HttpClient Error Handling**
- Added try-catch in HttpClient configuration
- Added timeout (30 seconds)
- Fallback to localhost if host extraction fails
- Better logging

### 3. **Login Error Handling**
- Specific handling for `HttpRequestException` (network errors)
- Specific handling for `TaskCanceledException` (timeouts)
- Prevents exceptions from crashing the runtime

## 🧪 Testing

1. **Restart the client:**
   ```bash
   cd SM_MentalHealthApp.Client
   dotnet run --launch-profile https
   ```

2. **From machine 2, try to login:**
   - Open browser console (F12)
   - Look for `🔐 Login:` messages
   - Check for any error messages

3. **Check the console output:**
   - Should see: `🌐 HttpClient BaseAddress configured: http://macip:5262/`
   - Should see login attempt messages
   - Should NOT see runtime crash

## 🔧 If Still Crashing

Check:
1. **Can machine 2 reach the server?**
   - Try: `http://macip:5262/api/health` in browser
   - Should return some response (even if 404)

2. **Check browser console:**
   - Look for CORS errors
   - Look for network errors
   - Look for the `🔐 Login:` messages

3. **Check server logs:**
   - Is the login request reaching the server?
   - Any errors in server console?

## 📝 What Changed

- **Program.cs**: Added error handling
- **DependencyInjection.cs**: Added HttpClient error handling and timeout
- **AuthService.cs**: Better exception handling for network errors

The runtime should no longer crash - errors will be caught and displayed as error messages instead.

