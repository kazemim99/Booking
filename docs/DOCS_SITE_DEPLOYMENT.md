# Docs-Site Deployment

How to deploy the **Docusaurus documentation site** (`docs-site/`) to GitHub Pages. This is about publishing the docs site itself — it has nothing to do with deploying the Booksy application; see the root `CLAUDE.md` for that.

**Docs URL**: `https://kazemim99.github.io/Booking/`

## Automated (recommended): GitHub Actions

`.github/workflows/deploy-docs.yml` builds and deploys automatically on every push to `master` that touches `docs-site/**`. Nothing to do beyond:

1. Edit docs in `docs-site/docs/`
2. Preview locally: `cd docs-site && npm start`
3. Commit and push to `master` — GitHub Actions builds, pushes to `gh-pages`, and the site updates within 2–5 minutes

## Manual deployment

If you need to deploy outside of CI:

```bash
cd docs-site
npm run build     # generates docs-site/build/
npm run deploy    # builds, pushes to gh-pages, and publishes
```

If `npm run deploy` fails (missing `gh-pages` package, permissions, etc.), fall back to one of these:

**git subtree** (from repo root):
```bash
cd docs-site
git subtree push --prefix build origin gh-pages
```

**Orphan branch** (first time, or if the subtree push fails):
```bash
git checkout --orphan gh-pages
git rm -rf .
cp -r docs-site/build/* .
git config user.email "noreply@github.com"
git config user.name "GitHub Pages"
git add .
git commit -m "Deploy documentation site"
git push origin gh-pages
git checkout master
```

**Ad-hoc preview** (no GitHub Pages config needed): drag `docs-site/build/` onto https://app.netlify.com/drop, or `vercel --prod docs-site/build` with the Vercel CLI.

## Enable GitHub Pages (one-time)

1. https://github.com/kazemim99/Booking/settings/pages
2. Source: **Deploy from a branch** → Branch: `gh-pages`, Folder: `/ (root)`
3. Save; wait 2–5 minutes; visit `https://kazemim99.github.io/Booking/`

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `npm run deploy` fails | `npm install --save-dev gh-pages` in `docs-site/`, then retry |
| Build fails | `cd docs-site && npm run build` locally to see the real error (usually a broken link or MDX syntax issue); fix and retry |
| `gh-pages` branch not found | `git switch --orphan gh-pages && rm -rf docs-site && touch .nojekyll && git add . && git commit -m "Initialize GitHub Pages" && git push -u origin gh-pages && git switch master`, then redeploy |
| Site not updating after 5+ minutes | Check the **Actions** tab for a failed `deploy-docs` run; as a cache-bust, toggle the Pages source branch away from and back to `gh-pages` in Settings → Pages |
| Rollback | `git revert <commit-hash> && git push origin master` — GitHub Actions redeploys the previous version |

## Files involved

- Source: `docs-site/docs/`
- Built output: `docs-site/build/` (gitignored, generated)
- Workflow: `.github/workflows/deploy-docs.yml`
- Config: `docs-site/docusaurus.config.ts` (site URL, base path, org/project — already set for `kazemim99/Booking`)
