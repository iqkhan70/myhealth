# Environment Separation Guide

## Overview

This project now has **complete separation** between Staging and Production environments to prevent accidental cross-environment deployments.

## File Structure

```
deploy/
├── DROPLET_IP_STAGING      # Staging server IP (dev branch)
├── DROPLET_IP_PRODUCTION   # Production server IP (main branch)
└── DROPLET_IP              # Legacy file (for other scripts)
```

## Deployment Rules

### 🟢 Staging Environment
- **Branch**: `dev`
- **IP File**: `deploy/DROPLET_IP_STAGING`
- **Workflow**: `.github/workflows/dev-staging.yml`
- **Auto-deploy**: ✅ Yes (on push to `dev`)
- **Approval Required**: ❌ No
- **Safety**: Branch validation + IP file validation

### 🔴 Production Environment
- **Branch**: `main`
- **IP File**: `deploy/DROPLET_IP_PRODUCTION`
- **Workflow**: `.github/workflows/production.yml`
- **Auto-deploy**: ✅ Yes (on push to `main`)
- **Approval Required**: ✅ Yes (GitHub environment protection)
- **Safety**: Branch validation + IP file validation + Manual approval

## Safety Features

### 1. Branch Validation
Each workflow **validates** it's running on the correct branch:
- `dev-staging.yml` → **MUST** run on `dev` branch (fails otherwise)
- `production.yml` → **MUST** run on `main` branch (fails otherwise)

### 2. IP File Validation
Each workflow checks that the correct IP file exists:
- Staging workflow → Checks for `DROPLET_IP_STAGING`
- Production workflow → Checks for `DROPLET_IP_PRODUCTION`

### 3. Clear Warnings
- Staging deployments show: `✅ STAGING Environment - this is safe`
- Production deployments show: `🚨 PRODUCTION Environment - WARNING!`

### 4. Production Protection
Production deployments require **manual approval** via GitHub environment protection.

## How It Prevents Disasters

### ❌ Before (Risky)
- Single `DROPLET_IP` file
- Both environments could use same IP
- No branch validation
- Easy to deploy to wrong environment

### ✅ Now (Safe)
- Separate IP files per environment
- Branch validation prevents wrong deployments
- Clear warnings show which environment
- Production requires manual approval
- Impossible to accidentally deploy prod when building dev

## Updating IP Addresses

### Update Staging IP
```bash
# Edit the staging IP file
nano deploy/DROPLET_IP_STAGING
# Change the IP address
# Commit and push to dev branch
```

### Update Production IP
```bash
# Edit the production IP file
nano deploy/DROPLET_IP_PRODUCTION
# Change the IP address
# Commit and push to main branch
```

## Workflow Behavior

### Pushing to `dev` branch:
1. ✅ Validates branch is `dev`
2. ✅ Loads `DROPLET_IP_STAGING`
3. ✅ Shows "STAGING Environment" warning
4. ✅ Deploys to staging server
5. ❌ **Cannot** deploy to production (different IP file)

### Pushing to `main` branch:
1. ✅ Validates branch is `main`
2. ✅ Loads `DROPLET_IP_PRODUCTION`
3. 🚨 Shows "PRODUCTION Environment" warning
4. ⏸️ **Requires manual approval**
5. ✅ Deploys to production server
6. ❌ **Cannot** deploy to staging (different IP file)

## Testing the Setup

### Test Staging Deployment
```bash
# Make a change and push to dev
git checkout dev
git commit -m "test staging deployment"
git push origin dev
# Should deploy to STAGING IP
```

### Test Production Deployment
```bash
# Make a change and push to main
git checkout main
git commit -m "test production deployment"
git push origin main
# Should require approval and deploy to PRODUCTION IP
```

## Troubleshooting

### Error: "This workflow should only run on 'dev' branch"
- **Cause**: You're trying to run staging workflow on wrong branch
- **Fix**: Make sure you're pushing to `dev` branch

### Error: "This workflow should only run on 'main' branch"
- **Cause**: You're trying to run production workflow on wrong branch
- **Fix**: Make sure you're pushing to `main` branch

### Error: "DROPLET_IP_STAGING file not found"
- **Cause**: The staging IP file is missing
- **Fix**: Create `deploy/DROPLET_IP_STAGING` with your staging IP

### Error: "DROPLET_IP_PRODUCTION file not found"
- **Cause**: The production IP file is missing
- **Fix**: Create `deploy/DROPLET_IP_PRODUCTION` with your production IP

## Best Practices

1. ✅ **Always** check which branch you're on before pushing
2. ✅ **Always** verify the IP in the workflow logs before deployment
3. ✅ **Never** manually edit IP files during active deployments
4. ✅ **Always** test staging changes before merging to main
5. ✅ **Always** review production deployments before approving

## Migration Notes

- The old `DROPLET_IP` file is kept for backward compatibility with other scripts
- GitHub Actions workflows now use environment-specific files
- Manual deployment scripts can still use `DROPLET_IP` if needed
