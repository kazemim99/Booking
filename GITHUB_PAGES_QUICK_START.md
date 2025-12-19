# GitHub Pages Deployment - Quick Start (5 minutes)

## TL;DR - Just Run These Commands

```bash
# 1. Build the docs
cd docs-site
npm run build

# 2. Deploy to GitHub Pages
npm run deploy

# 3. Wait 2-5 minutes
# 4. Visit: https://kazemim99.github.io/Booking/
```

**Done!** Your docs are now live. 🎉

---

## What Just Happened?

1. ✅ Your docs were built into static HTML/CSS/JS
2. ✅ Pushed to the `gh-pages` branch on GitHub
3. ✅ GitHub automatically published them to `https://kazemim99.github.io/Booking/`

## Next Steps

### Check It Worked
- Visit: https://kazemim99.github.io/Booking/
- Look for the Booksy documentation site
- Search some docs to verify it works

### (Optional) Verify GitHub Pages Settings

1. Go to: https://github.com/kazemim99/Booking/settings/pages
2. Should show:
   - **Source**: Deploy from a branch
   - **Branch**: `gh-pages / (root)`
   - **Status**: ✅ Your site is published at `https://kazemim99.github.io/Booking/`

### Update Your README

Add to [README.md](README.md):

```markdown
## Documentation

View the complete documentation at: **[https://kazemim99.github.io/Booking/](https://kazemim99.github.io/Booking/)**

Or run locally:
```bash
cd docs-site
npm start
```
```

### Future Updates

From now on, just:
1. Edit docs in `docs-site/docs/`
2. Commit and push to `master`
3. GitHub Actions automatically deploys! 🚀

No more manual deployment needed.

---

## If Something Goes Wrong

### Site not showing up after 5 minutes

```bash
# Try deploying again
cd docs-site
npm run deploy
```

### "npm run deploy" fails

```bash
# Install GitHub Pages tool
npm install --save-dev gh-pages

# Try again
npm run deploy
```

### Build errors

```bash
# Check what's wrong
cd docs-site
npm run build

# Fix any errors shown
# Then try deploy again
npm run deploy
```

---

## Support

- **Full Guide**: [GITHUB_PAGES_DEPLOYMENT.md](GITHUB_PAGES_DEPLOYMENT.md)
- **Docusaurus Docs**: https://docusaurus.io/docs/deployment
- **GitHub Pages Docs**: https://docs.github.com/en/pages

---

**Your docs URL**: `https://kazemim99.github.io/Booking/`
