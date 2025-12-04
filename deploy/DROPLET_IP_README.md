# Centralized Droplet IP Configuration

## Overview

All server IP addresses are now centralized in a single file: `deploy/DROPLET_IP`

This allows you to change the droplet IP in one place without modifying multiple files.

## Usage

### Changing the Droplet IP

Simply edit `deploy/DROPLET_IP`:

```bash
echo "YOUR_NEW_IP" > deploy/DROPLET_IP
```

### In Shell Scripts

All deployment scripts should source the IP loader:

```bash
#!/bin/bash

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Now use $DROPLET_IP or $SERVER_IP
ssh root@$DROPLET_IP "your-command"
```

### In GitHub Actions

The workflows automatically read from `deploy/DROPLET_IP`:

```yaml
- name: Load server IP
  run: |
    SERVER_IP=$(cat deploy/DROPLET_IP | tr -d '[:space:]')
    echo "SERVER_IP=$SERVER_IP" >> $GITHUB_ENV
```

### In Mobile App Config

For the mobile app, you'll need to update `MentalHealthMobileClean/src/config/app.config.js` manually, or create a build script that reads from `DROPLET_IP`.

## Files Updated

- ✅ `.github/workflows/dev-staging.yml` - Reads from DROPLET_IP
- ✅ `.github/workflows/production.yml` - Reads from DROPLET_IP
- ✅ `.github/workflows/initial-setup.yml` - Reads from DROPLET_IP
- ✅ `deploy/load-droplet-ip.sh` - Helper script for shell scripts

## Updating Existing Scripts

To update existing scripts to use the centralized IP:

1. **Manual update** (recommended for important scripts):
   - Add `source` line at the top
   - Replace hardcoded IPs with `$DROPLET_IP`

2. **Automatic update** (use with caution):
   ```bash
   ./deploy/update-scripts-with-ip.sh
   ```
   - Review changes before committing
   - Some scripts may need manual adjustment

## Example

**Before:**
```bash
DROPLET_IP="159.65.242.79"
ssh root@$DROPLET_IP "command"
```

**After:**
```bash
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
ssh root@$DROPLET_IP "command"
```

## Benefits

- ✅ Single source of truth for server IP
- ✅ Easy to switch between droplets
- ✅ No need to commit IP changes to git repeatedly
- ✅ Consistent IP across all scripts and workflows

