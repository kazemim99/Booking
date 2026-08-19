# Agent Instructions

Vendor-neutral entry point for any AI assistant working in this repository.

## Start here: the knowledge contract

Read **[docs/KNOWLEDGE.md](docs/KNOWLEDGE.md)** first. It states where each kind of knowledge
lives and which copy wins when two sources disagree. In short:

- **What the system is** → [openspec/project.md](openspec/project.md) — verified against source, cites its evidence
- **What it does** → the code, then `openspec/specs/<capability>/spec.md`
- **Why we chose it** → [ARCHITECTURAL_DECISIONS.md](ARCHITECTURAL_DECISIONS.md)
- **What's in flight** → `openspec/changes/`

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

In this repository, acceptance scenarios for ServiceCatalog behavior belong in the Reqnroll Gherkin features under `tests/Booksy.ServiceCatalog.IntegrationTests/` — see [docs/REQNROLL_TESTING.md](docs/REQNROLL_TESTING.md).

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

Follow the Test Pyramid. Priority order:

1. **Unit Tests** — domain logic, use cases, blocs/handlers, validators
2. **Integration Tests** — interaction between layers (API + DB + events); prefer realistic integration tests over excessive mocking
3. **Component/Widget Tests** — reusable UI components and screen state rendering
4. **End-to-End Tests** — only for critical user journeys, cross-screen workflows, and regression protection; avoid E2E where a lower-level test provides equivalent confidence

Choose the correct test types per change from: Unit, Integration, API, Widget, Golden (UI consistency), E2E, Regression, Contract, Database/Repository, State Management, Smoke, Performance, Accessibility, Concurrency/Race Condition. If a normally-expected test type is not applicable, state why. Do not generate unnecessary tests — select the minimal set that provides high confidence while keeping the suite maintainable.

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

