# Quick IP Update Guide

## Change Droplet IP in One Place

When you get a new droplet, update the IP in **one file**:

```bash
echo "YOUR_NEW_IP" > deploy/DROPLET_IP
```

## Update Everything Automatically

After changing `deploy/DROPLET_IP`, run:

```bash
./deploy/update-all-ips.sh
```

This updates:
- ✅ Mobile app config (`MentalHealthMobileClean/src/config/app.config.js`)
- ✅ GitHub Actions workflows (auto-read from `DROPLET_IP`)
- ✅ All deployment scripts (already use `load-droplet-ip.sh`)

## What's Already Updated

All deployment scripts now use:
```bash
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
```

Then they use `$DROPLET_IP` or `$SERVER_IP` variable.

## Files That Use Centralized IP

- ✅ All `.sh` scripts in `deploy/` directory
- ✅ GitHub Actions workflows (`.github/workflows/*.yml`)
- ✅ Mobile app config (via `update-mobile-config.sh`)

## Example

```bash
# 1. Update IP
echo "143.198.148.50" > deploy/DROPLET_IP

# 2. Update mobile config
./deploy/update-mobile-config.sh

# 3. Done! All scripts and workflows will use the new IP
```

## Verify

Check current IP:
```bash
cat deploy/DROPLET_IP
```

