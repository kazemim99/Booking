using Xunit;

// docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 5. Every class in this project now declares a
// [Collection] — BooksyHostTestCollection (SC + UM, slice 2 merged with UserManagement in slice 4)
// or HostCompositionCollection — so this flag no longer buys any safety within a collection:
// xUnit never runs two classes in the same collection concurrently regardless of this attribute.
// What it was still doing was forcing those two collections to run one after the other instead of
// side by side. They are safe to overlap: each gets its own factory, its own
// PostgresTestContainerFixture database on the shared server (never a shared database), and its
// own in-process DI container (caches, fakes, TestUserContext are per-factory, not static); the
// audit's own static-mutable-state sweep of src/ found none. xunit.runner.json caps
// maxParallelThreads at 2 — there are exactly two collections, so a higher cap would only let
// xUnit spin up threads with nothing to run.
//
// Splitting BooksyHostTestCollection itself into several smaller collections (the audit's original
// SC.Bookings/SC.Payments/SC.Providers/... breakdown) was not done: with host boot down to a ~1.6 s
// median (Phase 1) and the whole merged suite's own test time already at ~100-115 s for 480 tests
// (Phase 2 slice 4), splitting one host boot into five or more to shave seconds off an
// already-fast, already-stable run was judged not worth the added surface for background services
// (PaymentReconciliationBackgroundService, LedgerMaintenanceBackgroundService) and rate limiters to
// collide across more concurrently-running hosts. Tier 1, recorded in the change's Decisions.
[assembly: CollectionBehavior(MaxParallelThreads = 2)]
