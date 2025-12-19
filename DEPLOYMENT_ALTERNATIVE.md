# Alternative Deployment Method

If you're having issues with `npm run deploy`, use this manual method instead.

## Step 1: Build the Docs

```bash
cd docs-site
npm run build
```

This creates `docs-site/build/` with all your static files.

## Step 2: Option A - GitHub Pages (Manual via git)

### Create and Push to gh-pages Branch

```bash
# From repo root
cd docs-site
git subtree push --prefix build origin gh-pages
```

If that doesn't work, use this alternative:

```bash
# Create orphan gh-pages branch (first time only)
git checkout --orphan gh-pages

# Remove all files
git rm -rf .

# Add the build directory contents
cp -r docs-site/build/* .

# Commit
git config user.email "noreply@github.com"
git config user.name "GitHub Pages"
git add .
git commit -m "Deploy documentation site"

# Push
git push origin gh-pages

# Return to master
git checkout master
```

### Enable GitHub Pages

1. Go to https://github.com/kazemim99/Booking/settings/pages
2. Under "Source", select:
   - Branch: `gh-pages`
   - Folder: `/ (root)`
3. Click **Save**
4. Wait 2-5 minutes
5. Visit: https://kazemim99.github.io/Booking/

## Step 2: Option B - Netlify Drop (Easiest - No Configuration)

1. Go to https://app.netlify.com/drop
2. Drag and drop `docs-site/build/` folder
3. Your docs go live instantly!
4. You'll get a URL like: `https://xyz-abc.netlify.app`

## Step 2: Option C - Vercel

1. Install Vercel CLI: `npm install -g vercel`
2. Deploy: `vercel --prod docs-site/build`
3. Follow prompts
4. Your docs go live instantly!

## Step 2: Option D - Simple GitHub Workflow (Recommended)

Create a GitHub Actions workflow that handles it all automatically.

File: `.github/workflows/deploy-docs.yml` (already exists!)

1. Push your changes to `master`
2. GitHub Actions automatically:
   - Builds the docs
   - Pushes to `gh-pages`
   - Deploys to GitHub Pages
3. Your docs update within 2-5 minutes!

To enable:
1. Commit your changes
2. Push to master
3. Go to **Actions** tab in GitHub
4. Workflow runs automatically

---

## Troubleshooting

### "gh-pages branch not found"

The branch doesn't exist yet. Create it:

```bash
git switch --orphan gh-pages
rm -rf docs-site
touch .nojekyll
git add .
git commit -m "Initialize GitHub Pages"
git push -u origin gh-pages
git switch master
```

Then rebuild and push docs.

### Build folder is huge

That's normal! Docusaurus builds are 10-20MB. GitHub Pages handles it fine.

### Docs not showing after 5 minutes

Check GitHub Pages settings:
1. https://github.com/kazemim99/Booking/settings/pages
2. Should show green checkmark with deployment URL
3. If not, check **Actions** tab for failed workflow

### Want instant preview?

Use **Netlify Drop** (Option B above) - no setup needed!

---

## Recommended Path

1. **Quick test**: Use Netlify Drop (Option B)
2. **Permanent solution**: Use GitHub Actions workflow (Option D)

The GitHub Actions workflow we created automates everything!

---

## Files Involved

- Build source: `docs-site/docs/`
- Built output: `docs-site/build/`
- Workflow: `.github/workflows/deploy-docs.yml`
- Config: `docs-site/docusaurus.config.ts`
