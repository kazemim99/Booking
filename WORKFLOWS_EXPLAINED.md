# GitHub Workflows Explained

You have 3 workflows. Here's what each does:

## Workflow Comparison

| Workflow | Filename | When it Runs | What it Does |
|----------|----------|--------------|------------|
| **dotnet.yml** | `.github/workflows/dotnet.yml` | On every push to `master`, `develop`, `claude/**` | Only **builds** .NET code (tests disabled) |
| **deploy.yml** | `.github/workflows/deploy.yml` | On every push to `master` | Tests + Build Docker images + Deploy to server |
| **deploy-docs.yml** | `.github/workflows/deploy-docs.yml` | On push to `docs-site/` folder | Builds & deploys Docusaurus to GitHub Pages |

---

## Current Status: Which One is ACTUALLY Running?

### ✅ **dotnet.yml is the ACTIVE one** (Currently Running)

```
On: push to master/develop/claude/**
Triggers: Automatically
Does: Only builds .NET, no deployment
Branches: master, develop, claude/**
Tests: DISABLED (commented out)
Deployment: NONE
```

**This is a lightweight CI pipeline** - it just compiles your code.

---

## deploy.yml Status: **NOT RUNNING**

```
On: push to master
Triggers: Automatically (but something may be preventing it)
Does: Tests + Docker build + Deploy to server
Branches: master only
Tests: ENABLED
Deployment: YES (to your Ubuntu server)
```

**Why it may not be running:**
1. GitHub Secrets might not be set correctly
2. Workflow file might have an issue
3. It might be disabled

---

## Detailed Breakdown

### 1️⃣ dotnet.yml - **Currently Active** ✅

**Purpose**: Quick .NET CI build

**Triggers on**:
- Push to `master` branch
- Push to `develop` branch
- Push to `claude/**` branches
- Pull requests to `master` or `develop`
- Manual trigger via "Run workflow"

**What it does**:
```
1. Checkout code
2. Setup .NET 9.0
3. Restore dependencies (npm cache)
4. Build solution in Release mode
5. Done! ✓
```

**Tests**: All disabled (commented out at lines 48-97)
- Unit tests ❌ (commented)
- Integration tests ❌ (commented)
- Architecture tests ❌ (commented)

**Deployment**: None ❌

---

### 2️⃣ deploy.yml - **Should be Running** ⚠️

**Purpose**: Full CI/CD pipeline with deployment

**Triggers on**:
- Push to `master` branch only
- Manual trigger via "Run workflow"

**What it does**:
```
Job 1: Run Tests
  1. Checkout code
  2. Setup .NET
  3. Run unit tests ✓
  4. Run integration tests ✓ (with PostgreSQL, Redis)

Job 2: Build & Push Docker Images
  1. Build UserManagement API Docker image
  2. Build ServiceCatalog API Docker image
  3. Build API Gateway Docker image
  4. Build Frontend Docker image
  5. Push to GitHub Container Registry

Job 3: Deploy to Server
  1. SSH into your Ubuntu server
  2. Pull latest Docker images
  3. Stop old containers
  4. Start new containers
  5. Health check ✓
  6. Cleanup old images

Job 4: Notify Status
  1. Send success/failure notification
```

**Deployment**: Yes ✓ (to your Ubuntu server via SSH)

---

### 3️⃣ deploy-docs.yml - **New** (Docusaurus)

**Purpose**: Auto-deploy documentation website

**Triggers on**:
- Push to files in `docs-site/` folder
- Changes to `.github/workflows/deploy-docs.yml`
- Manual trigger via "Run workflow"

**What it does**:
```
Job 1: Build Docusaurus
  1. Checkout code
  2. Setup Node.js 20
  3. Install npm dependencies
  4. Build Docusaurus
  5. Upload artifacts

Job 2: Deploy to GitHub Pages
  1. Deploy to https://kazemim99.github.io/Booking/
```

**Deployment**: Yes ✓ (to GitHub Pages)

---

## Which One Should You Use?

### For Daily Development:
- **dotnet.yml** ✅ (lightweight, fast, always runs)
- Shows if code compiles
- Takes ~5 minutes

### For Production Deployment:
- **deploy.yml** ✅ (full CI/CD, needs setup)
- Tests your code
- Builds Docker images
- Deploys to server
- Takes ~10-15 minutes

### For Documentation:
- **deploy-docs.yml** ✅ (new, auto-deploys docs)
- Publishes to GitHub Pages
- Takes ~3 minutes

---

## Why Both Exist

```
Scenario 1: You push code to master
├─ dotnet.yml runs immediately
│  └─ Just builds .NET code (fast feedback)
│
└─ deploy.yml should also run
   └─ If tests pass → build Docker → deploy

Scenario 2: You push only docs changes
├─ dotnet.yml runs (but shouldn't)
│  └─ Wastes time on .NET build
│
└─ deploy-docs.yml runs
   └─ Builds and deploys docs only

Scenario 3: You push to feature branch
├─ dotnet.yml runs
│  └─ Builds code
│
└─ deploy.yml does NOT run
   └─ Only runs on master
```

---

## Current Issue: Why deploy.yml Might Not Be Running

### Check 1: Is it disabled?
1. Go to: `https://github.com/kazemim99/Booking/actions`
2. Look at the list of workflows
3. If "Deploy to Production" shows ⚠️ or "Disabled" - click it and enable

### Check 2: Are GitHub Secrets set?
1. Go to: `https://github.com/kazemim99/Booking/settings/secrets/actions`
2. You need these secrets:
   - `SERVER_HOST` ❌ (probably missing)
   - `SERVER_USER` ❌ (probably missing)
   - `SERVER_SSH_KEY` ❌ (probably missing)
   - `SERVER_DEPLOY_PATH` ❌ (probably missing)

If secrets are missing → deploy.yml won't work even if enabled

### Check 3: View the logs
1. Go to: `https://github.com/kazemim99/Booking/actions`
2. Click "Deploy to Production"
3. Click the failed run
4. See exactly what failed

---

## Recommendation: Optimize Your Workflows

### Option 1: Keep Both (Current)
- dotnet.yml: Runs on every push (fast feedback)
- deploy.yml: Runs on master only (full deployment)
- Problem: Tests run twice, slower feedback

### Option 2: Merge into One (Better)
- Combine dotnet.yml + deploy.yml
- Run tests → build Docker → deploy
- Only on master branch
- Faster, cleaner

### Option 3: Separate Concerns (Best)
- dotnet.yml: Test on all branches (PR checks)
- deploy.yml: Deploy on master only
- deploy-docs.yml: Docs on changes
- Problem: Current setup already does this, but tests are disabled in dotnet.yml

---

## What You Should Do Now

### If you want to deploy to your server:
1. ✅ Add GitHub Secrets (SERVER_HOST, SERVER_USER, SERVER_SSH_KEY, SERVER_DEPLOY_PATH)
2. ✅ Enable deploy.yml if disabled
3. ✅ Push code to master
4. ✅ Watch it deploy

### If you just want code to compile:
1. ✅ dotnet.yml is already working
2. ✅ It runs automatically
3. ✅ No action needed

### If you want to auto-deploy docs:
1. ✅ deploy-docs.yml is already created
2. ✅ Enable GitHub Pages in repo settings
3. ✅ Push to docs-site/ folder
4. ✅ It deploys automatically

---

## Monitor Your Workflows

**View all running workflows:**
```
https://github.com/kazemim99/Booking/actions
```

**View specific workflow results:**
```
dotnet.yml results: https://github.com/kazemim99/Booking/actions/workflows/dotnet.yml
deploy.yml results: https://github.com/kazemim99/Booking/actions/workflows/deploy.yml
deploy-docs.yml results: https://github.com/kazemim99/Booking/actions/workflows/deploy-docs.yml
```

**Recent run details:**
```
Click any workflow run to see:
- Build logs
- Test results
- Deployment status
- Error messages
```

---

## Summary Table

| Need | Workflow | Status |
|------|----------|--------|
| Build .NET code | dotnet.yml | ✅ Running |
| Deploy to server | deploy.yml | ⚠️ Needs secrets |
| Deploy docs | deploy-docs.yml | ✅ Ready |
| Tests on PR | dotnet.yml | ❌ Disabled |
| Tests on master | deploy.yml | ✅ Enabled |

---

Next step: Go to GitHub settings and add the 4 secrets for deploy.yml to work!
