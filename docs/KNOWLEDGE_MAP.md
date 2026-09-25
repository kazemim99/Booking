# Booking Knowledge Map

Which document answers which question, and which documents can still be trusted.

> **Read this before grepping the documentation.** The repository carries roughly **1 MB of
> markdown across ~465 tracked files** written over about ten months, and a large fraction of it
> describes a system that no longer exists. Search alone will confidently return a 2025 document
> about the retired microservices architecture.

**This is a map, not a copy.** It records *where* knowledge lives and *how much to trust it* —
never the knowledge itself. Always open the source before answering. If this map and a source
disagree, the source wins and this map is the bug. Everything here is re-derivable from
`git log` and `ls`; the commands are at the end of each section.


# Booking Knowledge Map

## When to Use

Load this **before** grepping the documentation, answering a question about how Booking works,
or citing any markdown file you found by search.

The repository carries roughly **1 MB of markdown across ~465 tracked files**, written over
about ten months. A large fraction of it describes a system that no longer exists. Grep alone
will confidently hand you a 2025 document about a microservices architecture that was retired.
This map exists so that does not happen.

## The one rule

**This skill is a map, not a copy.** It records *where* knowledge lives and *how much to trust
it* — never the knowledge itself. Always open the source file before answering. If this map and
a source disagree, the source wins and this map is the bug.

Consequently: never "answer from the map." Route with it, then read.

## Trust ladder

Freshness is measured by last commit touching the file, and it correlates strongly with accuracy
in this repo.

| Tier | Sources | How to treat them |
|---|---|---|
| **Verified** | [`openspec/project.md`]openspec/project.md) — every claim checked against source and citing its evidence | Authoritative for "what is this system". Start here. |
| **Current** (Aug 2026) | [`AGENTS.md`]AGENTS.md), [`ARCHITECTURAL_DECISIONS.md`]ARCHITECTURAL_DECISIONS.md), [`TECHNICAL_DOCUMENTATION.md`]TECHNICAL_DOCUMENTATION.md), [`PRODUCTION_READINESS_AUDIT.md`]PRODUCTION_READINESS_AUDIT.md), [`IDENTITY_AND_STAFF_ARCHITECTURE.md`]IDENTITY_AND_STAFF_ARCHITECTURE.md), `openspec/specs/`, `openspec/changes/` | Trust, but still verify specifics against code. |
| **Aging** (Jun–Jul 2026) | `API_ENDPOINTS.md`, `COMPLETION_ROADMAP.md`, `README.md`, `DTO_MAPPING.md`, `CHANGELOG.md`, `MONOLITH_MIGRATION_PLAN.md` | Directionally right; individual endpoints, DTOs, and statuses may have moved. Confirm against code. |
| **Stale** (2025) | Most of `docs/` — 32 of its 36 files predate 2026 | Historical. Useful for *why*, unreliable for *what is*. |
| **Do not trust** | `docs-site/` (untouched since 2025-12-22) and `docs/archive/` | `docs-site/` still documents RabbitMQ, an API gateway, and per-service hosts — all retired. `docs/archive/` is explicitly point-in-time. Never cite either as current behavior. |

Two known traps:

- **`docs/INDEX.md` is broken** — ten of its links point at files that no longer exist. Do not
  use it as a directory.
- **`README.md`'s "Recent Updates" is from 2025-12-21.** The banner is not a freshness signal.

## Routing table

| Question | Go to |
|---|---|
| What is this system? Stack, contexts, what's *not* present? | [`openspec/project.md`]openspec/project.md) |
| Why was it built this way? | [`ARCHITECTURAL_DECISIONS.md`]ARCHITECTURAL_DECISIONS.md) — ADR-001…007 |
| What does capability X do today? | `openspec/specs/<capability>/spec.md` (40 capabilities) |
| What work is in flight? | `openspec/changes/` — 4 active; 42 archived under `changes/archive/` |
| How does auth / OTP / registration / EF owned entities work? Known issues? | [`TECHNICAL_DOCUMENTATION.md`]TECHNICAL_DOCUMENTATION.md) — see its `## Known Issues & Solutions` |
| Is it ready to ship? What's blocking? | [`PRODUCTION_READINESS_AUDIT.md`]PRODUCTION_READINESS_AUDIT.md) |
| How do staff / memberships / identity work? | [`IDENTITY_AND_STAFF_ARCHITECTURE.md`]IDENTITY_AND_STAFF_ARCHITECTURE.md) + change `refactor-identity-and-membership` |
| What endpoints exist? | [`API_ENDPOINTS.md`]API_ENDPOINTS.md) *(aging)* → confirm against controllers in `src/**/Controllers/` |
| What are the rules for working here? | [`AGENTS.md`]AGENTS.md) — test-first standard + testing policy |
| Where does knowledge live, and which copy wins? | [`docs/KNOWLEDGE.md`]docs/KNOWLEDGE.md) |
| How do I run / deploy / debug it? | [`CLAUDE.md`]CLAUDE.md) — commands, Docker, health checks, troubleshooting |
| How do I write or run tests? | `AGENTS.md` (policy) · `CLAUDE.md` `## Test Suites` |
| How do we archive an OpenSpec change? | skill `openspec-change-lifecycle` |
| How do I confirm a claim is true? | skill `verify-before-claiming` |

## Where the code is

Do not infer structure from the docs — read [`openspec/project.md`]openspec/project.md),
which is verified against source. In brief: one ASP.NET Core host (`src/Host/AsanRezerve.Host`)
composing bounded contexts under `src/BoundedContexts/` and `src/UserManagement/`; four client
apps (`asanrezerve-frontend`, `asanrezerve-admin`, `asanrezerve-customer-app`, `asanrezerve-provider-app`); tests
under `tests/`.

## Full index

A file-by-file annotated index follows below.
it actually answers — is in
the Document Index below. Load it when the routing table above
does not resolve your question.

## Verification

```bash
git log -1 --format=%ad --date=short -- <file>   # freshness of any doc
grep -rl "TBD - created by archiving" openspec/specs/   # capabilities lacking a real Purpose
```

When a document turns out to be wrong, fix the document and update this map's tier for it —
do not simply remember the correction.

---

# Document Index


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
| 2026-07-12 | `VISUAL_STUDIO_DEBUGGING.md` | Running/debugging `AsanRezerve.Host` in VS against Docker infra. |
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
| 2026-07-12 | `DOCS_SITE_DEPLOYMENT.md` | Deploying the Docusaurus site — *not* about deploying AsanRezerve. |
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

`docs/archive/` (23 files) is explicitly historical — auth flow, booking cancellation/reschedule,
provider search, real-time availability, the Reqnroll migration and its 2026-09-11 retirement, the
original business proposal. Useful for *why*; never cite as current.

## `docs-site/` — do not trust

29 files, untouched since **2025-12-22**. Documents RabbitMQ, an API gateway, and per-service
hosts — all retired in the monolith migration. It is a publishing artifact, not a source of
truth. If it needs to be correct, that is its own piece of work.

## Per-app documentation

| Location | Answers |
|---|---|
| `asanrezerve-customer-app/` | `PROJECT_SUMMARY.md`, `FLUTTER_BACKEND_CONNECTION.md`, `CUSTOMER_APP_UX_FLOW.md` |
| `asanrezerve-provider-app/` | `DESIGN_LANGUAGE.md` — **normative** design spec (tokens, components); states Coliride is a visual-language donor only, never its flows. `AUTH_SPECIFICATION.md`. |
| `asanrezerve-frontend/e2e/` | `README.md` — Playwright suite conventions. |
| `asanrezerve-admin/` | Has its own `AGENTS.md`, `CLAUDE.md`, and nested `openspec/`. |

## Machine-local, not in git

Claude Code keeps per-project memory under `~/.claude/projects/c--Repos-Booking/memory/`. Hermes
keeps `MEMORY.md` / `USER.md` under `$HERMES_HOME/memories/` — which is `~/.hermes` on Linux and
macOS but **`%LOCALAPPDATA%\hermes` on Windows**; resolve it with `hermes config` rather than
assuming.

Both are **caches**. Per [`docs/KNOWLEDGE.md`]docs/KNOWLEDGE.md), nothing durable may
live only there — deleting either must cost nothing but re-derivation time. Hermes's memory is
additionally too small to hold project knowledge even if you wanted it to: `MEMORY.md` is capped
at ~2,200 characters and is shared across *every* project on the machine. It is for pointers
("Booking's knowledge contract is docs/KNOWLEDGE.md"), never for content.
