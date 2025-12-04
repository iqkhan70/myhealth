# CI/CD Setup Guide

This guide explains how to set up continuous integration and deployment (CI/CD) for your application.

## 📋 Overview

The CI/CD pipeline includes:

1. **Initial Setup** - One-time server configuration (run manually)
2. **Dev/Staging** - Auto-deploys on push to `dev` branch
3. **Production** - Auto-deploys on push to `main` branch (requires approval)
4. **Tests** - Runs on pull requests and pushes
5. **Mobile Builds** - Builds iOS/Android apps when mobile code changes

## 🔧 Setup Steps

### Step 1: Add GitHub Secrets

Go to your GitHub repository → **Settings → Secrets and variables → Actions** and add:

1. **SSH_PRIVATE_KEY**
   - Your SSH private key for accessing the DigitalOcean server
   - Get it: `cat ~/.ssh/id_rsa`
   - Copy the entire key (including `-----BEGIN` and `-----END` lines)

2. **EXPO_TOKEN** (optional, for mobile builds)
   - Get it from: https://expo.dev/accounts/[your-account]/settings/access-tokens
   - Only needed if you want to build mobile apps in CI/CD

### Step 2: Configure GitHub Environments (Optional)

For production deployments with approval:

1. Go to **Settings → Environments**
2. Create environment: `production`
3. Enable **Required reviewers** (add yourself/team)
4. This adds a manual approval step before production deployments

### Step 3: Branch Strategy

- **`dev` branch** → Auto-deploys to staging
- **`main` branch** → Auto-deploys to production (with approval if configured)

## 🚀 Workflows

### 1. Initial Server Setup (`initial-setup.yml`)

**When:** Manual trigger only

**What it does:**
- Runs your `myscript1.sh` and `myscript2.sh`
- Sets up server, database, nginx, certificates
- One-time setup for new servers

**How to run:**
1. Go to **Actions** tab in GitHub
2. Select **Initial Server Setup**
3. Click **Run workflow**
4. Type `yes` to confirm
5. Click **Run workflow**

### 2. Dev/Staging Deployment (`dev-staging.yml`)

**When:** Push to `dev` branch

**What it does:**
- Builds .NET server and client
- Runs tests (if configured)
- Deploys to staging server
- Runs database migrations
- Restarts application
- Health check

**Automatic:** Yes, on every push to `dev`

### 3. Production Deployment (`production.yml`)

**When:** Push to `main` branch

**What it does:**
- Builds .NET server and client
- Runs tests
- **Creates backup** before deployment
- Deploys to production
- Runs database migrations
- Restarts application
- Health check
- **Rolls back** if health check fails

**Automatic:** Yes, but requires approval if environment protection is enabled

### 4. Tests (`test.yml`)

**When:** Pull requests and pushes to `dev`/`main`

**What it does:**
- Builds the solution
- Runs unit tests
- Code quality checks

**Automatic:** Yes

### 5. Mobile Builds (`mobile-build.yml`)

**When:** Push to `dev`/`main` with changes in `MentalHealthMobileClean/`

**What it does:**
- Builds iOS app (on macOS)
- Builds Android app (on Ubuntu)
- Uploads build artifacts

**Automatic:** Yes, when mobile code changes

## 📝 Workflow Files

All workflow files are in `.github/workflows/`:

- `initial-setup.yml` - One-time server setup
- `dev-staging.yml` - Dev/staging deployments
- `production.yml` - Production deployments
- `test.yml` - Run tests
- `mobile-build.yml` - Build mobile apps

## 🔄 Typical Workflow

### Development Flow

1. **Create feature branch:**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes and commit:**
   ```bash
   git add .
   git commit -m "Add new feature"
   git push origin feature/my-feature
   ```

3. **Create pull request to `dev`:**
   - Tests run automatically
   - Review and merge

4. **Auto-deploy to staging:**
   - When merged to `dev`, staging deployment runs automatically
   - Test on staging server

5. **Deploy to production:**
   - Create PR from `dev` to `main`
   - Merge to `main`
   - Production deployment runs (may require approval)

### Hotfix Flow

1. **Create hotfix branch from `main`:**
   ```bash
   git checkout -b hotfix/critical-fix main
   ```

2. **Fix and merge to both branches:**
   ```bash
   # Fix the issue
   git commit -m "Fix critical bug"
   git push origin hotfix/critical-fix
   
   # Merge to main (production)
   # Then merge to dev (staging)
   ```

## 🛠️ Customization

### Add Tests

Edit `.github/workflows/test.yml`:

```yaml
- name: Run tests
  run: |
    dotnet test --no-build --verbosity normal
```

### Change Server IP

Edit the `SERVER_IP` in workflow files:

```yaml
env:
  SERVER_IP: your-server-ip
```

### Add More Environments

Create new workflow files for additional environments (e.g., `staging.yml`).

### Custom Deployment Steps

Add steps to workflows before/after deployment:

```yaml
- name: Run custom script
  run: |
    ssh user@server "your-command"
```

## 🔍 Monitoring

### View Workflow Runs

1. Go to **Actions** tab in GitHub
2. See all workflow runs
3. Click on a run to see logs

### Deployment Status

- ✅ Green checkmark = Success
- ❌ Red X = Failed
- 🟡 Yellow circle = In progress

### Rollback

If production deployment fails, it automatically rolls back to the previous backup.

## 🐛 Troubleshooting

### SSH Connection Fails

- Check `SSH_PRIVATE_KEY` secret is correct
- Verify server IP is correct
- Check firewall allows SSH

### Build Fails

- Check .NET version matches
- Verify all dependencies are restored
- Check build logs in Actions

### Deployment Fails

- Check server has enough disk space
- Verify application service is running
- Check server logs: `journalctl -u mental-health-app`

### Health Check Fails

- Application may need more time to start
- Check application logs
- Verify nginx is running

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET GitHub Actions](https://github.com/actions/setup-dotnet)
- [Expo GitHub Actions](https://github.com/expo/expo-github-action)

## ✅ Checklist

- [ ] Added `SSH_PRIVATE_KEY` secret
- [ ] Added `EXPO_TOKEN` secret (if building mobile apps)
- [ ] Configured production environment protection (optional)
- [ ] Tested initial setup workflow
- [ ] Pushed to `dev` branch to test staging deployment
- [ ] Verified staging deployment works
- [ ] Tested production deployment (with approval if configured)

