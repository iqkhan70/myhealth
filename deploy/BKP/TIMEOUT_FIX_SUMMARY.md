# Complete Timeout Fix Summary

## Problem

AI generation was timing out after ~190 seconds on DigitalOcean, even though Ollama was responding.

## Root Causes Found

1. **Client-side HttpClient**: 30-second timeout (FIXED: increased to 15 minutes)
2. **Nginx /api location**: No timeout settings (default 60s) (FIXED: added 900s)
3. **Kestrel server**: No timeout settings (FIXED: added 900s)
4. **Streaming read**: No timeout protection (FIXED: added 30s inactivity timeout)
5. **Ollama URL**: Using `localhost` instead of `127.0.0.1` (FIXED: set to `127.0.0.1:11434`)

## Fixes Applied

### 1. Client-Side (`DependencyInjection.cs`)

- ✅ HttpClient timeout: 30s → 15 minutes
- ✅ Fallback client timeout: 30s → 15 minutes

### 2. Server-Side Code (`ChainedAIService.cs`)

- ✅ Reduced `num_predict`: 512 → 256 tokens (faster generation)
- ✅ Added streaming timeout protection (30s inactivity timeout)
- ✅ Better cancellation token handling

### 3. Nginx Configuration

- ✅ Added `proxy_read_timeout 900s` to `/api` location
- ✅ Added `proxy_connect_timeout 900s`
- ✅ Added `proxy_send_timeout 900s`

### 4. Kestrel Configuration (`appsettings.Production.json`)

- ✅ Added `Kestrel.Limits.KeepAliveTimeout: 900`
- ✅ Added `Kestrel.Limits.RequestHeadersTimeout: 900`
- ✅ Added `RequestTimeout: "00:15:00"`

### 5. Ollama Configuration

- ✅ Set `Ollama.BaseUrl: "http://127.0.0.1:11434"` (not localhost)
- ✅ Created pre-load service to warm up model on boot
- ✅ Added keep-alive to prevent model unloading

## Scripts Created

1. `fix-all-timeouts-complete.sh` - Applies all timeout fixes
2. `setup-ollama-preload.sh` - Sets up automatic model pre-loading
3. `configure-ollama-keepalive.sh` - Configures Ollama keep-alive
4. `preload-ollama-model.sh` - Manually pre-loads model

## Next Steps

1. **Deploy the code changes:**

   ```bash
   # Build and deploy server
   cd SM_MentalHealthApp.Server
   dotnet publish -c Release
   # Then deploy to server
   ```

2. **Run the timeout fix script:**

   ```bash
   ./deploy/fix-all-timeouts-complete.sh
   ```

3. **Pre-load the model (optional but recommended):**

   ```bash
   ./deploy/preload-ollama-model.sh
   ```

4. **Test the AI generation** - it should now work without timing out!

## Expected Performance

- **First request (cold start)**: 30-60 seconds (model loading)
- **Subsequent requests**: 5-15 seconds (model in memory)
- **Much better than**: 190+ seconds with timeout errors

## Troubleshooting

If it still times out:

1. Check Nginx timeout:

   ```bash
   ssh root@159.89.34.48 "grep -A 15 'location /api' /etc/nginx/sites-available/mental-health-app | grep timeout"
   ```

2. Check Kestrel timeout:

   ```bash
   ssh root@159.89.34.48 "cat /opt/mental-health-app/server/appsettings.Production.json | python3 -c \"import sys, json; c=json.load(sys.stdin); print(c.get('Kestrel', {}))\""
   ```

3. Check Ollama:

   ```bash
   ssh root@159.89.34.48 "curl -s http://127.0.0.1:11434/api/tags"
   ```

4. Check logs:
   ```bash
   ssh root@159.89.34.48 "journalctl -u mental-health-app -f"
   ```

## Notes

- The 1vCPU/2GB droplet is the bottleneck - Ollama is slow on limited resources
- Consider upgrading to 2vCPU/4GB for better performance
- Or use a smaller model like `phi-2` or `qwen2.5:0.5b`
