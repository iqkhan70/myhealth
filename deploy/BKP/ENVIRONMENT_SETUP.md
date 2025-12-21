# Environment Configuration

This project uses separate IP addresses for Staging and Production environments.

## Files

- `deploy/DROPLET_IP_STAGING` - Staging server IP (used by `dev` branch)
- `deploy/DROPLET_IP_PRODUCTION` - Production server IP (used by `main` branch)

## Deployment Rules

### Staging (dev branch)
- **Branch**: `dev`
- **IP File**: `deploy/DROPLET_IP_STAGING`
- **Workflow**: `.github/workflows/dev-staging.yml`
- **Auto-deploy**: Yes (on push to dev)
- **Approval Required**: No

### Production (main branch)
- **Branch**: `main`
- **IP File**: `deploy/DROPLET_IP_PRODUCTION`
- **Workflow**: `.github/workflows/production.yml`
- **Auto-deploy**: Yes (on push to main)
- **Approval Required**: Yes (GitHub environment protection)

## Safety Features

1. **Branch Validation**: Each workflow validates it's running on the correct branch
2. **IP File Validation**: Workflows check that the correct IP file exists
3. **Clear Warnings**: Production deployments show clear warnings
4. **Environment Protection**: Production requires manual approval

## Updating IP Addresses

To update an IP address, edit the corresponding file:
- Staging: Edit `deploy/DROPLET_IP_STAGING`
- Production: Edit `deploy/DROPLET_IP_PRODUCTION`

**Important**: Make sure you're editing the correct file for the correct environment!
