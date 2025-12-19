# GitHub Pages Deployment Guide

Deploy your Booksy documentation to GitHub Pages in 5 minutes!

## Prerequisites

✅ GitHub repository (already have it: `kazemim99/Booking`)
✅ Node.js v22+ installed
✅ Git configured locally

## Step 1: Configuration (Already Done ✓)

The configuration has been updated:
- **URL**: `https://kazemim99.github.io`
- **Base URL**: `/Booking/`
- **Organization**: `kazemim99`
- **Project**: `Booking`

File: [docs-site/docusaurus.config.ts](docs-site/docusaurus.config.ts) (lines 18, 21, 25-26)

## Step 2: Build the Documentation

```bash
cd docs-site
npm run build
```

This generates static files in `docs-site/build/`

## Step 3: Deploy to GitHub Pages

### Option A: Using npm script (Recommended)

```bash
cd docs-site
npm run deploy
```

This command:
1. Builds the site
2. Creates/updates the `gh-pages` branch
3. Pushes to GitHub
4. Your docs go live automatically!

### Option B: Manual Deployment with Git

If the npm script doesn't work, deploy manually:

```bash
cd docs-site
npm run build

# Add the build output
git add -A
git commit -m "docs: deploy documentation to GitHub Pages"

# Create gh-pages branch (if it doesn't exist)
git subtree push --prefix docs-site/build origin gh-pages
```

## Step 4: Enable GitHub Pages

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Pages**
3. Under "Source", select:
   - Branch: `gh-pages`
   - Folder: `/ (root)`
4. Click **Save**

GitHub will automatically detect the change and deploy.

## Step 5: View Your Docs

After ~2-5 minutes, your documentation will be live at:

```
https://kazemim99.github.io/Booking/
```

## Verify Deployment

1. Check GitHub Actions:
   - Go to **Actions** tab in your repository
   - Look for the deployment workflow
   - Confirm it completed successfully

2. Check GitHub Pages:
   - Go to **Settings** → **Pages**
   - Should see a green checkmark with "Your site is published at..."

3. Visit the URL:
   - Navigate to `https://kazemim99.github.io/Booking/`
   - Should see the Docusaurus site

## Troubleshooting

### Deployment command doesn't work

Ensure you're using npm (not yarn):
```bash
npm --version  # Should be v10+
```

If using GitHub Actions (recommended), see the automated workflow below.

### Build fails

1. Check for errors:
   ```bash
   cd docs-site
   npm run build
   ```

2. Fix any build errors (usually broken links or MDX syntax)

3. Try again

### Pages not updating

1. Clear GitHub Pages cache:
   - Go to **Settings** → **Pages**
   - Change branch to `main`, save
   - Change back to `gh-pages`, save

2. Wait 5-10 minutes for rebuild

3. Check Actions tab for any failed workflows

## Automated Deployment with GitHub Actions

We've already created a workflow file: [.github/workflows/deploy-docs.yml](.github/workflows/deploy-docs.yml)

This automatically deploys whenever you push to `master`:

```yaml
name: Deploy Documentation
on:
  push:
    branches: [master]
    paths: ['docs-site/**']
  workflow_dispatch:
```

**To use it:**
1. Commit and push your changes
2. GitHub Actions automatically builds and deploys
3. Your docs update within 2-5 minutes

No manual deployment needed!

## Future Updates

Every time you update documentation:

```bash
# 1. Edit docs in docs-site/docs/
# 2. Test locally
cd docs-site
npm start

# 3. Commit and push
git add .
git commit -m "docs: update [section] documentation"
git push origin master

# 4. GitHub Actions automatically deploys! 🚀
```

## Rollback

If something goes wrong:

1. Revert your changes:
   ```bash
   git revert <commit-hash>
   git push origin master
   ```

2. GitHub Actions will redeploy the previous version

3. Your docs update within minutes

## Reference

- [Docusaurus Deployment Docs](https://docusaurus.io/docs/deployment)
- [GitHub Pages Docs](https://docs.github.com/en/pages)
- [GitHub Actions Docs](https://docs.github.com/en/actions)

---

**Your docs URL**: `https://kazemim99.github.io/Booking/`

Once live, update your README with this link!
