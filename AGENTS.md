# Agent Instructions

Vendor-neutral entry point for any AI assistant working in this repository.

## Source of truth — read this before answering anything about this repository

**Authority is ordered. When two sources disagree, the higher one wins.**

1. **The code, tests, and git history.** Final authority on what the system does. A document the
   code contradicts is wrong, by definition.

2. **[openspec/project.md](openspec/project.md) — the verified source of truth for what this
   system *is*.** Every claim in it was checked directly against source and cites the file that
   proves it, including an explicit *"Explicitly not present"* table. **Start here for any
   architecture question.**

3. **Specifications and decisions.** `openspec/specs/<capability>/spec.md` — what each capability
   does. [ARCHITECTURAL_DECISIONS.md](ARCHITECTURAL_DECISIONS.md) — why we chose it.
   `openspec/changes/` — what is in flight.

4. **Routing and index documents.** These tell you *where to look* and *what to trust* — never
   what is true: [docs/KNOWLEDGE.md](docs/KNOWLEDGE.md) (which source wins for which question)
   and [docs/KNOWLEDGE_MAP.md](docs/KNOWLEDGE_MAP.md) (which document answers what, and which are
   too stale to trust).

5. **Everything else is guidance, not authority** — `README.md`, `docs/**`, `docs-site/`, and
   `CLAUDE.md`. **`CLAUDE.md` is explicitly NOT authoritative.** It is Claude-Code-oriented
   operational guidance; where it and `openspec/project.md` disagree, `project.md` is correct.
   **Never cite `CLAUDE.md` as this repository's source of truth.**

**Follow this routing path before relying on search.** Read
[docs/KNOWLEDGE_MAP.md](docs/KNOWLEDGE_MAP.md) *before* grepping the documentation, and never
treat an incidental grep hit as an authoritative answer. Every file in `docs-site/` and the large
majority of `docs/` predates 2026, and many describe a retired microservices architecture — search
cannot tell you which. The map can, and it carries the commands to re-derive its own freshness.

**Any AI memory you carry is a cache, not a source of truth.** Verify it against git before
relying on it; if it disagrees with the code, the code is right. Nothing durable may live only
in agent memory — if a fact matters, propose adding it to the right file above.

## Test-First Development for Business Behavior Changes

For any non-trivial feature, domain behavior change, workflow change, or cross-boundary modification, follow a test-first approach.

The implementation workflow must be:

1. Understand and document the business behavior.
2. Define acceptance scenarios before writing production code.
3. Create or update tests that describe the expected behavior.
4. Implement the minimum production changes required to satisfy those tests.
5. Refactor while keeping all behavioral tests green.

The goal is not test coverage percentage. The goal is protecting business rules and system behavior.

### Test Selection Rules

Choose the appropriate test level based on the type of change.

#### Integration Tests are mandatory for:

- Domain workflows spanning multiple components.
- State transitions.
- Persistence behavior.
- Database-backed business rules.
- API contracts.
- Background jobs.
- Event publishing.
- Real-time/message/event flows (CAP integration events).
- Payment and financial workflows.
- Availability/slot computation, booking lifecycle, provider onboarding and approval, and ledger/settlement behavior.
- Any feature where multiple bounded contexts interact.

Integration tests should verify the complete business scenario, not individual implementation details.

Examples:

- Provider registration → staff invitation → service publication → customer booking.
- Booking lifecycle changes (confirm → reschedule → cancel → refund).
- Availability computation → persistence → API exposure.
- State machine transitions.
- Notification triggering.

In this repository, acceptance scenarios for ServiceCatalog behavior are xUnit integration tests under `tests/Booksy.Host.IntegrationTests/ServiceCatalog/` — one test class per business area, named for the scenario it proves. Reqnroll/Gherkin BDD was retired 2026-09-11: 95% of its scenarios had never run (unbound steps), and the rest were duplicated by, or ported into, xUnit tests — see `openspec/changes/_inline/retire-reqnroll/tasks.md`.

#### Unit Tests are preferred for:

Pure deterministic logic and isolated rules, including:

- Calculators.
- Policies.
- Value objects.
- Validators.
- Algorithms.
- Mathematical models.
- Time window calculations.
- Pricing formulas.

Unit tests should validate business invariants and edge cases.

Examples:

- Extending a service's duration must never increase the number of bookable slots in a fixed window.
- Adding a staff break must never increase available capacity.
- A refund must never exceed the amount actually captured for a booking.

#### End-to-End Tests are reserved for:

Critical user journeys where validating the complete stack provides unique value.

Avoid replacing integration tests with excessive E2E tests.

### Acceptance Scenarios Before Implementation

Before implementing a significant feature:

- Identify the affected business capabilities.
- Write explicit Given/When/Then scenarios.
- Identify existing behavior that must remain unchanged.
- Identify edge cases and failure scenarios.
- Confirm ambiguous business decisions before coding.

A feature is not complete until the important business scenarios are covered by automated tests.

### Protect Business Rules, Not Implementation Details

Tests should describe:

- What the system must do.
- What users/business expect.
- What invariants must always hold.

Tests should NOT:

- Lock unnecessary implementation details.
- Be rewritten only to make new code pass.
- Duplicate production code logic.

When requirements change, update the scenarios first, then update implementation.

### Investigation Before Implementation

For existing systems, before modifying code:

1. Inspect the current architecture.
2. Identify existing capabilities.
3. Identify current behavior through code and tests.
4. Identify compatibility risks.
5. Propose the smallest safe change.

Do not implement based only on a feature description or meeting notes.

### Incremental Delivery

Prefer:

- Small vertical slices.
- Backward-compatible changes.
- Additive API changes.
- Feature flags/dark launches where appropriate.
- Migration paths with rollback strategies.

Avoid large changes that combine:

- new business rules,
- architecture refactoring,
- and unrelated cleanup

in a single step.

### Definition of Done

A significant feature is complete only when:

- Acceptance scenarios exist.
- Relevant integration tests exist.
- Unit tests cover pure logic.
- Existing behavior remains protected.
- API/event contracts are validated where applicable.
- The implementation matches the approved business rules.

## Testing Policy

The Testing Policy below is the operational detail of the test-first principle above — the two are one policy, not alternatives. Where they overlap, the test-first workflow governs *when* tests are written; this section governs *how*.

Testing is mandatory, not optional. Quality is a non-negotiable requirement: every change must maintain or improve the project's reliability, and every feature is incomplete until all relevant automated tests pass. Every code change must include appropriate automated tests unless there is a documented technical reason why a specific test type is not applicable.

### Existing Tests First

Whenever modifying existing code:

- First search for all existing tests related to the affected functionality.
- Prefer updating and extending existing tests instead of creating duplicate test files.
- Follow the project's existing testing architecture, naming conventions, folder structure, fixtures, helper utilities, and testing frameworks.
- Keep the test suite clean, maintainable, and consistent with the rest of the project.
- Avoid duplicate or redundant tests unless they provide additional coverage or protect against a different regression.
- If existing test coverage is insufficient, extend it before creating entirely new test suites.
- Explain which existing tests were reviewed, which were modified, and why new tests were necessary.
- Never introduce code changes without verifying that the relevant existing tests still pass.
- If a change affects business logic, APIs, data access, events, caching, background jobs, or concurrency, ensure the existing test suite is updated accordingly.
- At the end of every implementation, include a Testing Summary that lists: Existing tests reviewed, Tests modified, New tests added, Test types covered, Regression risks addressed.

### Test Integrity — Never Weaken the Suite

- Existing tests must NEVER be removed, weakened, skipped, or disabled simply to make CI pass. No ignored failures, no commented-out assertions.
- A test may be removed only when it is demonstrably obsolete (the behavior it protected no longer exists). Explain why, and replace it with equivalent or stronger protection when the behavior has a successor.
- When business logic changes: first identify every existing test affected by the change, update those tests to the new behavior, then add new tests for the new behavior. Do this before declaring the change done.

### Test Pyramid & Test Selection

Ask these three questions in order. The first "yes" is the level.

1. **Can the behaviour be observed with no I/O** — on a domain object, a handler with its ports
   substituted, a validator, a specification, a controller with a substituted mediator? → **unit test**.
   The mechanical form of this rule: *if the test needs Testcontainers, `WebApplicationFactory` or a
   `DbContext`, it is not a unit test — and if it does not, it does not belong in an integration project.*
2. **Does correctness depend on something only the real stack provides** — EF mapping and conventions,
   SQL constraints, transactions and concurrency, the CAP outbox, middleware and the response envelope,
   auth policy, the composed DI graph, the HTTP contract? → **integration test against `Booksy.Host`**.
   Write **one per endpoint per outcome class** (success, unauthenticated, forbidden, not found, one
   representative 400) plus one per business scenario that genuinely spans components. Validation
   permutations are unit tests of the validator, not fifteen HTTP round-trips.
3. **Does the value come from the whole deployed stack** — real network, real OTP sandbox, real browser?
   → **E2E**, and only for the keystone journeys already scripted.

A bug fix's regression test goes at the **lowest level that reproduces it**. Component/widget tests in
Flutter and Vue follow the same three questions inside their own stacks (see *Mobile App Testing*).

Choose the correct test types per change from: Unit, Integration, API, Widget, Golden (UI consistency), E2E, Regression, Contract, Database/Repository, State Management, Smoke, Performance, Accessibility, Concurrency/Race Condition. If a normally-expected test type is not applicable, state why. Do not generate unnecessary tests — select the minimal set that provides high confidence while keeping the suite maintainable.

### Test Infrastructure Rules

These are enforced mechanically where they can be; the rest are review items. Rationale and the
remaining roadmap: [docs/TEST_ARCHITECTURE_AUDIT.md](docs/TEST_ARCHITECTURE_AUDIT.md).

- **No sleeping, no wall clock, no unseeded randomness** in test code. `tests/BannedSymbols.txt` turns
  `Task.Delay`, `Thread.Sleep`, `DateTime.Now/Today` and `Random.Shared` into build warnings; a seeded
  `new Random(seed)` stays legal because a property test's seeds are part of its contract. To assert
  that something moved a timestamp, back-date the stored value — do not sleep for the clock.
- **One version per package.** Test projects take their versions from `tests/Directory.Packages.props`;
  a `Version=` attribute in a test csproj is a mistake. FluentAssertions stays on the 7.x line: 8.0
  moved to a commercial licence.
- **A test that cannot pass is not skipped.** `[Fact(Skip = …)]` states nothing and rots — both skips
  removed in 2026-09 were still blaming a defect that had been fixed months earlier, while the real
  cause (the endpoint returns a paged envelope) went unrecorded. Fix it, or record it as a known
  failure with a measured reason (`tests/known-failures.txt` header).
- **Test hosts run as `Testing`** and load `appsettings.Testing.json`: no seeding
  (`Database:SeedOnStartup=false`), quiet logging, in-memory cache. Configuration that shapes the test
  environment belongs in that file, not in a stack of `UseSetting` calls in a base class.
- **Every FULL run records per-test timings** (`.verify/trx/`, `.verify/slowest.txt`). The first test of
  a class carries that class's fixture; a multi-second "first test" means the class boots a host.

### Mandatory Engineering & Testing Policy

Every code change must prioritize correctness, reliability, maintainability, and regression prevention over implementation speed.

**1. Analyze before coding.** Before making any change: understand the existing business logic, identify all affected components, determine the risk of the change, and identify which tests must be added, updated, or verified. Never modify code until you understand the existing behavior.

**2. Mandatory testing.** Every change must be validated with the appropriate level of testing, selected per the Test Pyramid above.

**3. Regression prevention.** Every bug fix MUST follow this order: (a) reproduce the bug with a failing test, (b) implement the fix, (c) verify the new test passes, (d) verify no existing tests broke. Never fix a bug without adding regression protection.

**4. Business logic coverage.** Every new or modified business rule must be covered by automated tests, including: happy paths, failure scenarios, edge cases, boundary values, invalid inputs, error handling, exception paths, authorization/permission rules, null and empty values, time-based behavior, state transitions, and concurrent execution / race conditions (when applicable).

**5. Verify side effects.** After every implementation, verify the change does not unintentionally affect: backward compatibility, API compatibility, database behavior, event publishing/consumption, background jobs, caching, authentication & authorization, logging, error handling, performance.

**6. Testing-first mindset.** Before implementing, explain which tests already exist, which should be added, and why each is necessary. After implementing, write/update the required tests and ensure existing tests still pass. Do not consider a task complete until the appropriate automated tests are included.

**7. Completion checklist.** Every completed task must end with a report containing: Files Changed, Tests Added, Tests Updated, Test Types Covered, Scenarios Verified, Potential Risks, Recommended Future Tests (if any). Never state a task is complete unless the required tests have been implemented, or you explicitly explain why a particular test is unnecessary.

### Test Execution & Completion Workflow

At the end of every implementation, in order:

1. Determine which test types the change requires (per the Test Pyramid).
2. Implement or update those tests.
3. Execute all affected tests — Unit, Integration, Widget, and any impacted E2E — plus static analysis (`flutter analyze` for the mobile app, analyzers/linters for .NET and the frontend).
4. Investigate and fix the root cause of any failure — never work around it.
5. Verify no regressions were introduced elsewhere in the suite.
6. Only then consider the task complete.

### Test Quality Standards

Tests must be readable, deterministic, isolated/independent, fast, maintainable, and self-documenting (descriptive names; one behavior per test). Use Arrange-Act-Assert. Prefer factories/builders over large inline objects. Mock only external dependencies — never business logic; prefer real implementations in integration tests.

Avoid: flaky tests, arbitrary sleeps/delays (await deterministic conditions instead), fragile/unstable selectors (use `data-testid` on web, `Key`/finder-by-type in Flutter), shared mutable state between tests, duplicated setup, and magic numbers.

### Coverage Expectations

Target meaningful coverage that protects business behavior — never write tests just to raise a percentage. Expected minimums: business logic and core services 90%+, state management (blocs/cubits) 90%+, repositories 80%+, critical user flows 100% via integration and/or E2E tests. Coverage numbers alone never indicate quality.

### Mobile App Testing (`booksy-customer-app`)

Run with `flutter analyze` (must be error-free) and `flutter test`. In addition to the general policy, validate whenever the change touches them:

- **Navigation & deep links**: router redirects, per-tab back stacks, Android back behavior, return-to-intent (`test/config/routes/app_router_test.dart` is the pattern)
- **Authentication**: OTP flow, session restore, guest gating, auth-state transitions
- **Booking flows**: step transitions, selection preservation, slot-taken recovery, cancel/reschedule optimistic updates + rollback (see `test/features/booking/`, `test/features/bookings/`)
- **Payments**: when payment flows land, they are critical-path — integration + E2E required
- **State management**: every Bloc/Cubit gets dedicated tests — initial state, transitions, failure paths, retry behavior, race conditions (stale-result guards), unexpected user actions
- **Widget rendering**: every reusable `core/widgets` component — rendering, interactions, disabled/loading/error states, accessibility labels, theming (see `test/core/widgets/widgets_test.dart`)
- **Screen states**: loading skeleton / content / empty / error via `StateSwitcher`; pull-to-refresh
- **Offline & network failures**: offline banner, fail-fast `NetworkFailure` mapping, retry logic
- **Localization & RTL**: Persian strings from `AppStrings` only; RTL rendering; layouts must not break under LTR
- **Accessibility**: semantics labels, ≥48dp touch targets, 1.3× font scale without overflow, reduced-motion (`disableAnimations`)
- **Screen sizes & dark mode**: no overflow at small widths; when dark theme ships, both themes are tested
- **App lifecycle**: state restoration and background/foreground transitions for flows holding in-progress state (e.g. the booking flow)
- **Push notifications**: if/when added, cover receipt-driven navigation and permission states
- **Performance-critical flows**: list scrolling with images, search debouncing — verify no jank-inducing rebuilds in hot paths

Known constraint: `build_runner` codegen is currently broken (retrofit_generator/SDK incompatibility) — new services use manual JSON parsing and manual `get_it` registration in `core/di/injection.dart`; do not add `@JsonSerializable`/`@injectable` code that requires regeneration until the toolchain is fixed.

### CI & Release Quality Gates

Every Pull Request must leave the project in a releasable state:

- No failing tests; no skipped critical tests; no ignored failures; no known-flaky tests merged.
- Static analysis clean: `flutter analyze` (mobile), Roslyn analyzers/StyleCop (backend), ESLint (frontend). Fix warnings you introduce; never suppress diagnostics to silence them.
- Security: no secrets in code or config committed to the repo; authorization rules covered by tests when auth-adjacent code changes; validate all external input at API boundaries.
- Architecture: respect the existing layering (Clean Architecture / DDD boundaries; presentation → domain → data in Flutter). New code follows the established patterns of its module — deviations require an explicit, documented reason.
- Performance: for changes on hot paths (queries, list rendering, event handlers), state the expected impact and verify it — no unbounded queries, no N+1s, no per-frame allocations in scroll paths.
- Migrations remain idempotent; deploy gates (keystone E2E) must stay green.

## Operating Model — own the task from intake to a verified finish

This section governs every turn. It grants autonomy over execution and over reversible
engineering decisions. It grants none over product behavior, money, security or privacy
semantics, irreversible production data, or anything that leaves the working tree.
Mechanics that enforce it: `scripts/verify` (checkable "done"), the `Stop` hook in
`.claude/settings.json` (blocks ending a turn while work remains), and the `ask` permission list
(protected operations). See [docs/AUTONOMOUS_OPERATING_MODEL.md](docs/AUTONOMOUS_OPERATING_MODEL.md).

### The loop

1. **Intake.** Feature → OpenSpec change under `openspec/changes/<id>/`. Bug or small change →
   inline task list at `openspec/changes/_inline/<slug>/tasks.md`. Either way the loop state is a
   file, never the conversation.
2. **Investigate** per *Investigation Before Implementation*. Write down what must not change.
3. **Plan** `tasks.md` in the contract format below: acceptance scenarios, then small tasks.
4. **Execute.** For each open task: write or extend the failing test → implement the minimum →
   `scripts/verify -Tier fast` → fix the root cause until green → check the task off in its own
   edit → record any decision taken. Repeat while open tasks remain.
5. **Finish.** `scripts/verify -Tier full` green → `tasks.md` reflects reality → `Status: DONE`
   → one report.

Do not stop between tasks to summarize, to ask whether to proceed, or to seek approval for an
item on an already-approved list. The proposal is the approval. A task being large, tedious, or
spanning many files is not a reason to stop.

### Decisions — four tiers

Rule: if a git revert undoes it and it is inside the approved scope, decide it; if not, ask.

- **Tier 0, decide silently:** naming, placement, following the module's existing pattern,
  behavior-preserving refactors.
- **Tier 1, decide and record:** engineering trade-offs within the spec — pattern choice, test
  level, defaults, internal shapes, where a guard lives. One line under `## Decisions`.
- **Tier 2, decide, record, flag:** clear bug fixes and corrections of broken behavior (broken
  endpoints, incorrect results, architecture defects), legacy cleanup, development migrations
  (written and applied to local/test databases only), additive public API changes, hot-path
  performance trade-offs, new dependencies, documented-pattern deviations. Recorded under
  `## Decisions`, and listed in the final report.
- **Tier 3, park and ask:** genuinely unresolved decisions about **product or business
  behavior** the spec does not define or that alters an approved scenario; **money or payment
  semantics**; **security or privacy semantics** (authn/authz rules, exposure, retention);
  **irreversible production data loss** (destructive migrations against shared data, ledger
  rewrites, deleting or weakening tests); **genuinely ambiguous UX requirements**.

A behavior change is not tier 3 because it is visible; it is tier 3 because nobody has decided
it and the choice is the business's to make. "Needs judgment" is tier 1 or 2; "needs someone
else's authority" is tier 3. Tier 3 parks the task as `[?] DECISION: <question>` and the loop
continues with every unblocked task. The run ends only when nothing unblocked remains.

### Protected operations — mechanical, not judgment

These prompt for confirmation through the `ask` list in `.claude/settings.json` regardless of
tier: `git push`, opening or merging PRs, `dotnet ef database update`, production/staging
compose, `ssh`/`scp`. Writing a migration is free; applying one to a shared database asks.
Secrets are never committed, printed, or faked with a realistic-looking placeholder.

**Shared machine.** Several assistant sessions may work in this checkout at once. Never kill a
process you did not start — a `testhost` or `dotnet` holding the build output is almost always
a peer's run, and killing it silently corrupts their result. When a build fails on a file lock,
list the sessions (`ListAgents`), message the peer, and take turns on `dotnet build`/`test`.
Re-read a file before editing it if it may have changed under you, and prefer exact-match edits
over whole-file writes on files a peer may hold open.

### Stop conditions — name the one that fired

1. **Done.** Every task `[x]`, or `[-]`/`[?]` with its reason recorded; nothing unblocked
   remains; `scripts/verify -Tier full` green; `Status: DONE`.
2. **Decision pending** (tier 3) and no unblocked task remains → `Status: STOPPED(decision)`.
3. **Blocked** on something the environment cannot produce (credential, external service,
   broken toolchain) and no unblocked task remains → `Status: STOPPED(blocked)`.
4. **Budget exhausted:** three failed attempts at the *same* failure. Park it as `[-] BLOCKED:`
   with the actual error and continue; stop only if nothing else is left →
   `Status: STOPPED(budget)`.

### `tasks.md` contract

First line `Status: ACTIVE|DONE|STOPPED(<condition>)`; second line `Verify: FAST|FULL` (the
tier a task must pass before it is checked off). Sections in order: `## Acceptance scenarios`,
`## Tasks`, `## Decisions`, `## Log`. Tasks are one line each, ≤ 160 characters, imperative,
independently verifiable. States: `[ ]` open, `[x]` done, `[-]` blocked (`BLOCKED: <reason>`),
`[?]` awaiting a tier-3 decision (`DECISION: <question>`). Never `[~]` — split partial work into
a done line and an open line. Narrative goes in `## Log`, dated, newest first; checkbox lines
never grow. Check a task off in its own edit, right after its verification passes. The file must
describe reality after a crash at any point. The `Stop` hook reads this file: it blocks ending
the turn while `[ ]` lines remain or the FULL verify is missing, stale, or red, and it honours
an explicit `Status: DONE` or `Status: STOPPED(...)`.

### Verification tiers

- **FAST** (after each task): `dotnet build Booksy.sln` + every unit and architecture test
  project. No Docker required. Plus the affected integration test class(es) when the task
  touched persistence, API, or events. The build runs once; every `dotnet test` after it passes
  `--no-build` (re-evaluating the project graph per project cost ~135 s of every FULL run).
- **FULL** (at finish): FAST + Host composition + both integration suites (Testcontainers
  Postgres, Docker required) + `type-check`/`lint:check` in each touched Vue app +
  `flutter analyze`/`flutter test` in each touched Flutter app.
- **Known-failure baseline.** `tests/known-failures.txt` lists integration tests that were
  already red on the committed baseline before a change (83 on 2026-09-09, measured in a clean
  worktree). A db step passes when every failure is on that list and fails on any failure off
  it. The list is debt: remove lines as tests are fixed; never add one to make a run green — a
  new failure is a regression until proven otherwise. `flutter analyze` is fatal on errors only
  (`--no-fatal-warnings --no-fatal-infos`), matching *Mobile App Testing* ("must be error-free").
- `scripts/verify` writes `.verify/status.json`; a result is only current for the exact
  working tree it ran against. Unrun tests are not a finish. A red run is reported red.
- **Per-test timings.** A FULL run also writes `.verify/trx/*.trx` and `.verify/slowest.txt` (the 20
  slowest tests). The first test of a class carries that class's fixture, so a multi-second first
  test is a class booting a host, not a slow test. `--blame-hang-timeout 5m` turns a hung
  concurrency test into a dump with a stack instead of a silent wait.

### Reporting

One report at the end: the stop condition that fired; what changed; the Testing Summary
required by *Existing Tests First*; tier-2 decisions and tier-3 questions; anything parked and
why. Report failures as failures, with the output — never "complete with a minor issue
outstanding".

<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

