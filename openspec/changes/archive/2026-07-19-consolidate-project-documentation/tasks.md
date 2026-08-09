## 1. Prep

- [x] 1.1 Create `docs/` and `docs/archive/` directories
- [x] 1.2 `grep -rl` across `README.md`, `CLAUDE.md`, `.github/workflows/`, and any other kept root doc for links to the 29 files being moved/merged, so every reference gets updated in step 5 — found one: `tests/Booksy.ServiceCatalog.IntegrationTests/Features/README.md` linking `REQNROLL_MIGRATION_PLAN.md`. Also found `docs-site/docs/testing/*.md` has its own separate manually-maintained copies of the Reqnroll docs — out of scope, left untouched.

## 2. Merge near-duplicate docs

- [x] 2.1 Merge `GEOLOCATION_DEBUGGING_GUIDE.md` + `README_GEOLOCATION_IRAN.md` → `GEOLOCATION_GUIDE.md` (root); dedupe overlapping sections, keep both the "how it works" and "how to debug" content; delete the two originals. Also redacted a live-looking Neshan API key that was hardcoded in the original debugging guide's curl example.
- [x] 2.2 Merge `VISUAL_STUDIO_DEBUG.md` + `VISUAL_STUDIO_DEBUGGING_WITH_DOCKER.md` → `VISUAL_STUDIO_DEBUGGING.md` (root); delete the two originals. Both originals described the retired Ocelot Gateway + separate UserManagement/ServiceCatalog API + RabbitMQ setup — rewrote against the current single-host modular-monolith setup (verified against `docker-compose.infrastructure.yml`, `Booksy.Host`'s `launchSettings.json`/`appsettings.json`) instead of just concatenating stale content.
- [x] 2.3 Merge `DEPLOYMENT_ALTERNATIVE.md` + `GITHUB_PAGES_DEPLOYMENT.md` + `GITHUB_PAGES_QUICK_START.md` → `docs/DOCS_SITE_DEPLOYMENT.md`; add a one-line header clarifying this covers the `docs-site/` static site, not the Booksy app; delete the three originals
- [x] 2.4 Merge `REQNROLL_QUICKSTART.md` + `REQNROLL_TEST_COVERAGE.md` → `docs/REQNROLL_TESTING.md`; delete the two originals. The coverage doc's exact scenario/feature-file counts (20 files, Nov 2025) were stale against the current 40 feature files — replaced the hand-maintained tally with a qualitative overview plus a pointer to `dotnet test --list-tests`.

## 3. Archive completed/historical docs

- [x] 3.1 For each of the 22 files (recount — originally miscounted as 21) listed in `proposal.md`'s "Move completed/historical write-ups" bullet: opened it, confirmed it describes a feature that has since shipped (not an open TODO/requirements doc), then `git mv`'d it into `docs/archive/` and prepended a one-line archive banner
- [x] 3.2 One file contradicted the "historical" classification: `PROVIDER_SEARCH_IMPLEMENTATION_GUIDE.md` was left "IN PROGRESS - Partial Implementation" in Nov 2025 with unchecked TODO tasks, and is not tracked in the current `COMPLETION_ROADMAP.md` — it looks abandoned/deprioritized, not shipped. Archived anyway (root doesn't need a dormant partial proposal cluttering it) but with a corrected banner explaining it was never completed, instead of the standard "superseded by shipped code" banner.

## 4. Verify the kept root set

- [x] 4.1 Skimmed `TECHNICAL_DOCUMENTATION.md` (already has an accurate monolith-migration disclaimer at the top, dated session entries explicitly marked historical — no changes needed) and `GALLERY_BACKEND_REQUIREMENTS.md` (genuine open requirements doc for gallery moderation — no changes needed)
- [x] 4.2 Confirmed `AGENTS.md` untouched

## 5. Update CLAUDE.md and cross-links

- [x] 5.1 Rewrote the "📚 Developer Documentation" section of `CLAUDE.md` into root / `docs/` / `docs/archive/` tiers
- [x] 5.2 Added a "Test Suites" section to `CLAUDE.md` covering the Playwright E2E suite, Reqnroll BDD tests, the bash API keystone smoke test, Cypress, and backend unit tests (kept separate from the existing "Testing Policy" section, which is about engineering process, not which suites exist)
- [x] 5.3 Fixed the link in `tests/Booksy.ServiceCatalog.IntegrationTests/Features/README.md`

## 6. Validate

- [x] 6.1 `git status` shows clean renames (`R`) for all 22 archived files (content preserved, only the banner line added) plus the 4 new merged files and 8 deletions — no unintended content loss
- [x] 6.2 Root directory listing shows exactly the 12 kept `.md` files (10 unchanged + `GEOLOCATION_GUIDE.md` + `VISUAL_STUDIO_DEBUGGING.md`), matching the proposal
