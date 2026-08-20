---
name: booking-knowledge-map
description: Routing map for Booking's ~465 markdown documents — which source answers which question, and which sources are current versus stale. Use before searching the docs, before answering an architecture or history question, and before trusting any document found by grep.
version: 1.0.0
metadata:
  hermes:
    tags: [documentation, navigation, booking, knowledge]
    category: project-knowledge
---

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
| **Verified** | [`openspec/project.md`](../../../openspec/project.md) — every claim checked against source and citing its evidence | Authoritative for "what is this system". Start here. |
| **Current** (Aug 2026) | [`AGENTS.md`](../../../AGENTS.md), [`ARCHITECTURAL_DECISIONS.md`](../../../ARCHITECTURAL_DECISIONS.md), [`TECHNICAL_DOCUMENTATION.md`](../../../TECHNICAL_DOCUMENTATION.md), [`PRODUCTION_READINESS_AUDIT.md`](../../../PRODUCTION_READINESS_AUDIT.md), [`IDENTITY_AND_STAFF_ARCHITECTURE.md`](../../../IDENTITY_AND_STAFF_ARCHITECTURE.md), `openspec/specs/`, `openspec/changes/` | Trust, but still verify specifics against code. |
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
| What is this system? Stack, contexts, what's *not* present? | [`openspec/project.md`](../../../openspec/project.md) |
| Why was it built this way? | [`ARCHITECTURAL_DECISIONS.md`](../../../ARCHITECTURAL_DECISIONS.md) — ADR-001…007 |
| What does capability X do today? | `openspec/specs/<capability>/spec.md` (40 capabilities) |
| What work is in flight? | `openspec/changes/` — 4 active; 42 archived under `changes/archive/` |
| How does auth / OTP / registration / EF owned entities work? Known issues? | [`TECHNICAL_DOCUMENTATION.md`](../../../TECHNICAL_DOCUMENTATION.md) — see its `## Known Issues & Solutions` |
| Is it ready to ship? What's blocking? | [`PRODUCTION_READINESS_AUDIT.md`](../../../PRODUCTION_READINESS_AUDIT.md) |
| How do staff / memberships / identity work? | [`IDENTITY_AND_STAFF_ARCHITECTURE.md`](../../../IDENTITY_AND_STAFF_ARCHITECTURE.md) + change `refactor-identity-and-membership` |
| What endpoints exist? | [`API_ENDPOINTS.md`](../../../API_ENDPOINTS.md) *(aging)* → confirm against controllers in `src/**/Controllers/` |
| What are the rules for working here? | [`AGENTS.md`](../../../AGENTS.md) — test-first standard + testing policy |
| Where does knowledge live, and which copy wins? | [`docs/KNOWLEDGE.md`](../../../docs/KNOWLEDGE.md) |
| How do I run / deploy / debug it? | [`CLAUDE.md`](../../../CLAUDE.md) — commands, Docker, health checks, troubleshooting |
| How do I write or run tests? | `AGENTS.md` (policy) · `docs/REQNROLL_TESTING.md` (BDD) · `CLAUDE.md` `## Test Suites` |
| How do we archive an OpenSpec change? | skill `openspec-change-lifecycle` |
| How do I confirm a claim is true? | skill `verify-before-claiming` |

## Where the code is

Do not infer structure from the docs — read [`openspec/project.md`](../../../openspec/project.md),
which is verified against source. In brief: one ASP.NET Core host (`src/Host/Booksy.Host`)
composing bounded contexts under `src/BoundedContexts/` and `src/UserManagement/`; four client
apps (`booksy-frontend`, `booksy-admin`, `booksy-customer-app`, `booksy-provider-app`); tests
under `tests/`.

## Full index

A file-by-file annotated index — every root doc and every file in `docs/`, with its date and what
it actually answers — is in
[references/document-index.md](references/document-index.md). Load it when the routing table above
does not resolve your question.

## Verification

```bash
git log -1 --format=%ad --date=short -- <file>   # freshness of any doc
grep -rl "TBD - created by archiving" openspec/specs/   # capabilities lacking a real Purpose
```

When a document turns out to be wrong, fix the document and update this map's tier for it —
do not simply remember the correction.
