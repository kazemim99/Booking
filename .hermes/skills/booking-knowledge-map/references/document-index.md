# Booking Document Index

File-by-file map of the markdown corpus. **Dates are the last commit touching the file** — the
single best available proxy for whether the content still describes reality.

Nothing here is a substitute for opening the file. This index tells you *whether it is worth
opening* and *what it claims to answer*.

Regenerate the dates with:

```bash
for f in *.md docs/*.md; do echo "$(git log -1 --format=%ad --date=short -- "$f") $f"; done | sort -r
```

---

## Root — living documents

| Date | File | Answers |
|---|---|---|
| 2026-08-19 | `AGENTS.md` | **The rules.** Knowledge contract + test-first standard + full testing policy. The file every AI assistant loads. |
| 2026-08-19 | `CLAUDE.md` | Repository overview, architecture summary, Docker/deploy commands, health checks, resource limits, troubleshooting, `## Test Suites`. Defers to `AGENTS.md` for policy. |
| 2026-08-16 | `TECHNICAL_DOCUMENTATION.md` | Largest technical doc (62 KB). Auth & phone verification, provider registration flow, location/maps, event-driven design, EF Core owned-entity config, routing guards, **`## Known Issues & Solutions`** (15 numbered issues), session history. |
| 2026-08-09 | `ARCHITECTURAL_DECISIONS.md` | **ADR-001…007**, newest first. Ledger as source of truth, payment money-safety, owned-key `ValueGeneratedNever`, GiST slot integrity, resource authorization, decoupled ledger posting, spec-driven hardening. Each records what it replaced and why. |
| 2026-08-09 | `PRODUCTION_READINESS_AUDIT.md` | Ship-readiness verdict with P0/P1 blockers, what is proven, what is not. |
| 2026-08-09 | `IDENTITY_AND_STAFF_ARCHITECTURE.md` | Person → OrganizationMembership → StaffProfile design: current problems, target model, UX flows, domain model. Pairs with change `refactor-identity-and-membership`. |
| 2026-07-16 | `API_ENDPOINTS.md` | Endpoint reference by context. *Aging* — confirm against `src/**/Controllers/`. |
| 2026-07-14 | `COMPLETION_ROADMAP.md` | Phased plan to MVP with epics and status. *Aging* — statuses drift fastest. |
| 2026-07-12 | `VISUAL_STUDIO_DEBUGGING.md` | Running/debugging `Booksy.Host` in VS against Docker infra. |
| 2026-07-12 | `GEOLOCATION_GUIDE.md` | Homepage location auto-detection: behavior, testing, debugging. |
| 2026-06-19 | `CHANGELOG.md` | Release history. *Aging.* |
| 2026-06-17 | `README.md` | Business overview, stack, project structure, getting started. **Its "Recent Updates" banner says 2025-12-21 — not a freshness signal.** |
| 2026-06-17 | `MONOLITH_MIGRATION_PLAN.md` | The microservices → modular-monolith migration. Explains *why* older docs are wrong. |
| 2026-06-17 | `DTO_MAPPING.md` | Backend C# DTO ↔ Flutter Dart ↔ Vue TS mapping, type conversion rules. *Aging.* |
| 2026-06-17 | `GALLERY_BACKEND_REQUIREMENTS.md` | Open requirements for provider-gallery admin moderation. |

## `openspec/` — specifications and change history

| Path | Answers |
|---|---|
| `openspec/project.md` | **Verified** system description — stack, persistence, messaging, API surface, conventions, and an explicit *"Explicitly not present"* table listing things older docs falsely claim. Start every architecture question here. |
| `openspec/specs/<capability>/spec.md` | What each of 40 capabilities does, as requirements + scenarios. Note: most still carry a placeholder `## Purpose`. |
| `openspec/changes/<id>/` | In-flight work: `proposal.md` (why/what/impact), `tasks.md` (checklist — **often stale**), `design.md`, `specs/` deltas. 4 active. |
| `openspec/changes/archive/YYYY-MM-DD-<id>/` | 42 completed changes. Excellent for *why a thing was built*; not a statement of current behavior. |
| `openspec/AGENTS.md` | How to use OpenSpec here — workflow, CLI, spec format, delta operations. |
| `openspec/reference/` | The detail split out of the above: authoring, examples, troubleshooting, conventions. |

## `docs/` — mostly historical

Four 2026 files; the other 32 predate 2026. Treat pre-2026 entries as *why*, never *what is*.

| Date | File | Answers |
|---|---|---|
| 2026-08-19 | `KNOWLEDGE.md` | **The knowledge contract** — where each kind of knowledge lives, which copy wins. |
| 2026-08-19 | `SERVICE_CATEGORY_MODEL.md` | Provider/service category model. |
| 2026-07-12 | `REQNROLL_TESTING.md` | Writing and running the Gherkin BDD integration tests. |
| 2026-07-12 | `DOCS_SITE_DEPLOYMENT.md` | Deploying the Docusaurus site — *not* about deploying Booksy. |
| 2025-12-18 | `INDEX.md` | **Broken** — 10 dead links. Do not use as a directory. |
| 2025-12-18 | `BOOKING_API_REFERENCE.md`, `BOOKING_MIGRATION_CHECKLIST.md` | Booking API and migration checklist, pre-monolith. |
| 2025-12-07 | `STAFF_INVITATION_FLOW.md`, `OTP_INVITATION_FLOW.md`, `HIERARCHY_MIGRATION_README.md` | Staff invitation and the **retired** provider-hierarchy model. Superseded by `IDENTITY_AND_STAFF_ARCHITECTURE.md`. |
| 2025-11-30 | `STAFF_*` (5 files), `ROLE_BASED_NAVIGATION_IMPLEMENTATION.md`, `PHONE_NUMBER_VALIDATION_REQUIREMENTS.md`, `OTP_TROUBLESHOOTING.md`, `MAP_COORDINATES_MISSING_ISSUE.md`, `LOCATIONS_API_OPTIMIZATION.md`, `GALLERY_ROW_VERSION_FIX_APPLIED.md`, `BACKEND_OTP_IMPLEMENTATION_GUIDE.md` | Point-in-time implementation write-ups. Historical. |
| 2025-11-26 | `WORKING_HOURS_VALIDATION.md`, `README.md`, `CACHE_INVALIDATION_PROBLEM_AND_SOLUTION.md` | Validation rules, docs directory readme, cache-invalidation fix. |
| 2025-11-21 | `VIEWMODEL_*`, `TYPE_CONSOLIDATION_GUIDE.md`, `BACKEND_DTO_CONSOLIDATION_GUIDE.md` | Naming/consolidation plans. Mostly executed; read as history. |
| 2025-11-20 | `UX_ROLE_BASED_NAVIGATION.md`, `QUICK_REFERENCE_ROLE_NAVIGATION.md` | Role-navigation UX design. |
| 2025-11-15/16 | `DATABASE_SCHEMA_UPDATES.md`, `CREATE_BOOKING_MODAL.md`, `CALENDAR_VIEW.md` | Schema and Vue UI write-ups. |
| 2025-11-08 | `ZarinPal-Sandbox-Testing-Guide.md` | ZarinPal sandbox testing. Relevant to `checkout-release-gates`. |
| 2025-11-01 | `api-design-notes.md` | Early API design notes. |

`docs/archive/` (22 files) is explicitly historical — auth flow, booking cancellation/reschedule,
provider search, real-time availability, the Reqnroll migration, the original business proposal.
Useful for *why*; never cite as current.

## `docs-site/` — do not trust

29 files, untouched since **2025-12-22**. Documents RabbitMQ, an API gateway, and per-service
hosts — all retired in the monolith migration. It is a publishing artifact, not a source of
truth. If it needs to be correct, that is its own piece of work.

## Per-app documentation

| Location | Answers |
|---|---|
| `booksy-customer-app/` | `PROJECT_SUMMARY.md`, `FLUTTER_BACKEND_CONNECTION.md`, `CUSTOMER_APP_UX_FLOW.md` |
| `booksy-provider-app/` | `DESIGN_LANGUAGE.md` — **normative** design spec (tokens, components); states Coliride is a visual-language donor only, never its flows. `AUTH_SPECIFICATION.md`. |
| `booksy-frontend/e2e/` | `README.md` — Playwright suite conventions. |
| `booksy-admin/` | Has its own `AGENTS.md`, `CLAUDE.md`, and nested `openspec/`. |

## Machine-local, not in git

Claude Code keeps per-project memory under `~/.claude/projects/c--Repos-Booking/memory/`. Hermes
keeps `MEMORY.md` / `USER.md` under `$HERMES_HOME/memories/` — which is `~/.hermes` on Linux and
macOS but **`%LOCALAPPDATA%\hermes` on Windows**; resolve it with `hermes config` rather than
assuming.

Both are **caches**. Per [`docs/KNOWLEDGE.md`](../../../../docs/KNOWLEDGE.md), nothing durable may
live only there — deleting either must cost nothing but re-derivation time. Hermes's memory is
additionally too small to hold project knowledge even if you wanted it to: `MEMORY.md` is capped
at ~2,200 characters and is shared across *every* project on the machine. It is for pointers
("Booking's knowledge contract is docs/KNOWLEDGE.md"), never for content.
