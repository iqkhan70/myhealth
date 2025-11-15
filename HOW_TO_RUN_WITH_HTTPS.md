# How to Run with HTTPS for Agora Calls

## 🎯 Current Setup

You're accessing:
- **Machine 1**: `http://localhost:5282` ✅ (works - localhost exception)
- **Machine 2**: `http://mac_ip:5282` ❌ (fails - not localhost, not HTTPS)

## ✅ Solution: Use HTTPS

### Step 1: Run Client with HTTPS Profile

```bash
cd SM_MentalHealthApp.Client
dotnet run --launch-profile https
```

### Step 2: Access via HTTPS

- **Machine 1**: `https://localhost:5282`
- **Machine 2**: `https://mac_ip:5282`

### Step 3: Accept Self-Signed Certificate

When you first access via HTTPS, your browser will show a security warning:
- Click **"Advanced"** or **"Show Details"**
- Click **"Proceed to localhost"** or **"Accept the Risk"**
- This is safe for development - it's just a self-signed certificate

## 🔍 Why This is Needed

**Agora SDK Security Requirements:**
- ✅ HTTPS (secure context) - **Works**
- ✅ localhost/127.0.0.1 (exception) - **Works**
- ❌ HTTP on IP address - **Fails** (not secure, not localhost)

## 📝 Alternative: Use Both HTTP and HTTPS

The launch settings now support both:
- **HTTPS**: `https://0.0.0.0:5282` (for Agora calls)
- **HTTP**: `http://0.0.0.0:5283` (for regular browsing)

You can access via either, but **Agora calls will only work on HTTPS**.

## 🚀 Quick Start

1. **Stop your current client** (if running)
2. **Run with HTTPS:**
   ```bash
   cd SM_MentalHealthApp.Client
   dotnet run --launch-profile https
   ```
3. **Access via HTTPS:**
   - Machine 1: `https://localhost:5282`
   - Machine 2: `https://YOUR_MAC_IP:5282`
4. **Accept the certificate warning** (first time only)
5. **Test a call** - it should work now! 🎉

## ⚠️ Important Notes

- **Self-signed certificates** are fine for development
- **Production** should use proper SSL certificates (Let's Encrypt, etc.)
- **Both machines** need to access via HTTPS for calls to work
- The **certificate warning** is normal for self-signed certs

