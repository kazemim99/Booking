## Why

The repo root has 38 markdown files with no organizing structure: point-in-time implementation write-ups from November 2025 ("BOOKING_CANCELLATION_IMPLEMENTATION.md", "PROVIDER_SEARCH_IMPLEMENTATION_GUIDE.md"), duplicated troubleshooting notes (two geolocation docs, two Visual Studio debugging docs, three GitHub Pages/docs-site deployment docs, three Reqnroll docs), and status snapshots for work that has since shipped ("INTEGRATION_TESTING_GUIDE.md" — "Build Verified", Nov 2025). A new contributor (or Claude) reading the root has no signal for which of the 38 files describe current behavior versus a completed one-off task. `CLAUDE.md` itself is accurate but doesn't mention the Playwright/Reqnroll test suites at all, and its doc links point at a flat, unfiltered list.

This is a documentation/tooling change, not a product capability — no spec deltas apply.

## What Changes

- Merge near-duplicate docs into one file each:
  - `GEOLOCATION_DEBUGGING_GUIDE.md` + `README_GEOLOCATION_IRAN.md` → root `GEOLOCATION_GUIDE.md`
  - `VISUAL_STUDIO_DEBUG.md` + `VISUAL_STUDIO_DEBUGGING_WITH_DOCKER.md` → root `VISUAL_STUDIO_DEBUGGING.md`
  - `DEPLOYMENT_ALTERNATIVE.md` + `GITHUB_PAGES_DEPLOYMENT.md` + `GITHUB_PAGES_QUICK_START.md` → `docs/DOCS_SITE_DEPLOYMENT.md` (this trio documents deploying the `docs-site/` static site, not the Booksy app — kept out of root to avoid confusion with app deployment)
  - `REQNROLL_QUICKSTART.md` + `REQNROLL_TEST_COVERAGE.md` → `docs/REQNROLL_TESTING.md` (Reqnroll is actively used — `.feature` files exist under `tests/Booksy.ServiceCatalog.IntegrationTests/Features/`)
- Move completed/historical write-ups to `docs/archive/` (each gets a one-line "Archived: superseded by shipped code, kept for history" banner at the top — 22 files, not 21 as originally miscounted): `AUTHENTICATION_FLOW_DOCUMENTATION.md`, `AUTHENTICATION_QUICK_REFERENCE.md`, `BOOKING_CANCELLATION_IMPLEMENTATION.md`, `BOOKING_RESCHEDULING_IMPLEMENTATION.md`, `BOOKSY_UX_ANALYSIS_AND_SEED_API_GUIDE.md`, `CQRS_COMPONENT_INVENTORY.md`, `EXECUTIVE_SUMMARY.md`, `FIGMA_DESIGN_PROMPT.md`, `IMPLEMENTATION_PRIORITY_ROADMAP.md`, `INTEGRATION_TESTS.md`, `INTEGRATION_TESTING_GUIDE.md`, `MIGRATION_GUIDE_WEEK1-2.md`, `PROVIDER_ACCESS_UX.md`, `PROVIDER_PROFILE_API_IMPLEMENTATION.md`, `PROVIDER_SEARCH_IMPLEMENTATION_GUIDE.md` (got a corrected banner instead of the standard one — it was left "IN PROGRESS" in Nov 2025 and isn't tracked by `COMPLETION_ROADMAP.md`, so it's archived as abandoned/unvalidated, not as shipped), `QUICK_TEST_GUIDE.md`, `REALTIME_AVAILABILITY_IMPLEMENTATION.md`, `REMAINING_IMPLEMENTATION_STEPS.md`, `REQNROLL_MIGRATION_PLAN.md`, `SOLO_PROVIDER_HANDLING.md`, `UNIFIED_AUTHENTICATION.md`, `booksy_business_proposal_srd.md`.
- Keep unchanged, at root, as the living/current doc set: `README.md`, `CLAUDE.md`, `CHANGELOG.md`, `AGENTS.md` (openspec-managed, do not touch), `API_ENDPOINTS.md`, `DTO_MAPPING.md`, `TECHNICAL_DOCUMENTATION.md`, `COMPLETION_ROADMAP.md`, `MONOLITH_MIGRATION_PLAN.md`, `GALLERY_BACKEND_REQUIREMENTS.md`.
- Rewrite `CLAUDE.md`'s "📚 Developer Documentation" section to reflect the new root/`docs/`/`docs/archive/` layout.
- Add to `CLAUDE.md`: a short "Testing" section covering the Playwright E2E suite (`booksy-frontend/e2e/`, see [add-playwright-e2e](../changes/add-playwright-e2e/) and the in-flight [harden-e2e-test-coverage](../changes/harden-e2e-test-coverage/) changes) and the Reqnroll BDD integration tests (`tests/Booksy.ServiceCatalog.IntegrationTests/`) — neither is currently mentioned in `CLAUDE.md`.
- Before moving/merging any file, skim its current content (not just its filename/date) to confirm the historical/duplicate classification still holds — a file touched in June 2026 by an unrelated bulk commit could still be stale, and a Nov-2025 file could still be a living reference.

## Capabilities

### New Capabilities
- `project-documentation`: a lightweight, repo-process capability (not a user-facing product feature) capturing the tiered doc layout and the expectation that `CLAUDE.md` documents the testing setup. Added only because `openspec validate --strict` requires at least one delta; this change is otherwise tooling-only and archives with `--skip-specs`.

### Modified Capabilities
None.

## Impact

- **Moved**: 21 files → `docs/archive/`.
- **Merged**: 8 files → 4 new files (2 stay at root, 2 land in `docs/`).
- **Unchanged**: 10 root docs (README, CLAUDE.md, CHANGELOG, AGENTS.md, API_ENDPOINTS, DTO_MAPPING, TECHNICAL_DOCUMENTATION, COMPLETION_ROADMAP, MONOLITH_MIGRATION_PLAN, GALLERY_BACKEND_REQUIREMENTS).
- **Modified**: `CLAUDE.md` doc-index section and a new "Testing" section.
- **No code or runtime changes.** Any in-repo links to moved files (check `README.md`, `CLAUDE.md`, other kept docs, and `.github/workflows/*.yml` for doc references) must be updated to the new paths.
- Archive normally (`openspec archive consolidate-project-documentation --yes`) so the lightweight `project-documentation` capability syncs into `openspec/specs/`.
