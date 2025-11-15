# Agora Project Troubleshooting Guide

## 🔍 What We Know

From your logs:
- ✅ App ID is correct: `efa11b3a7d05409ca979fb25a5b489ae`
- ✅ Token format is correct: starts with `006`
- ✅ App IDs match between token and parameter
- ❌ **But Agora servers reject it**: "invalid vendor key, can not find appid"

This means the issue is **NOT in your code** - it's in your **Agora project configuration**.

## ✅ Step-by-Step Checklist

### 1. **Check Project Status**
1. Go to https://console.agora.io/
2. Login to your account
3. Navigate to **Projects** → **Mental Health App**
4. **Check the status**:
   - ✅ Should be **"Active"** (green)
   - ❌ If **"Disabled"** or **"Suspended"** → Enable it
   - ❌ If **"Pending"** → Wait for activation

### 2. **Check App ID Activation**
1. In your project, go to **Project Management**
2. Click on your **App ID**: `efa11b3a7d05409ca979fb25a5b489ae`
3. Check which **services are enabled**:
   - ✅ **Real-Time Communication (RTC)** - MUST be enabled
   - ✅ **Video Calling** - Should be enabled
   - ✅ **Audio Calling** - Should be enabled
4. If any are disabled, **enable them**

### 3. **Verify Certificate**
1. In project settings, check **Certificates**
2. **Primary Certificate** should be: `89ab54068fae46aeaf930ffd493e977b`
3. Check if it's **Active** (not disabled)
4. **Copy it again** from console to ensure no extra spaces

### 4. **Check Token Authentication**
1. In project settings, find **Token Authentication**
2. **If enabled**: Tokens are required (your setup is correct)
3. **If disabled**: You can test without tokens first
4. **Recommendation**: Keep it enabled for security

### 5. **Check Account Status**
1. Go to **Account Settings**
2. Check:
   - ✅ Account is **Active**
   - ✅ No **billing issues**
   - ✅ **Quota limits** not exceeded
   - ✅ No **suspension notices**

### 6. **Check Project Region**
1. In project details, note the **Region**
2. Make sure you're using the correct region endpoints
3. Some regions have different requirements

## 🧪 Quick Test: Try Without Token

To isolate if it's a token issue:

1. **Temporarily disable Token Authentication** in Agora Console
2. **Modify your code** to join without a token (just App ID)
3. **Test the call**
4. **If it works**: Token generation issue
5. **If it still fails**: Project/App ID issue

## 🔧 Most Likely Issues

Based on the error pattern:

### Issue #1: Project Disabled (Most Likely)
**Symptom**: "can not find appid" from Agora servers
**Solution**: Enable the project in Agora Console

### Issue #2: RTC Service Not Activated
**Symptom**: App ID exists but RTC calls fail
**Solution**: Enable Real-Time Communication service

### Issue #3: Certificate Inactive
**Symptom**: Token generated but rejected
**Solution**: Activate Primary Certificate

### Issue #4: Account/Billing Issue
**Symptom**: All requests rejected
**Solution**: Check account status and billing

## 📞 Contact Agora Support

If everything looks correct in the console but it still fails:

1. Go to https://console.agora.io/
2. Click **Support** or **Help**
3. Create a ticket with:
   - App ID: `efa11b3a7d05409ca979fb25a5b489ae`
   - Error: "invalid vendor key, can not find appid"
   - Project name: "Mental Health App"
   - Screenshot of project status

## 🎯 Next Steps

1. **Check Agora Console** using the checklist above
2. **Share what you find** - especially:
   - Project status (Active/Disabled)
   - RTC service status (Enabled/Disabled)
   - Certificate status (Active/Inactive)
3. **Try the no-token test** if possible
4. **Contact Agora Support** if everything looks correct

The error is definitely on Agora's side, not your code!

