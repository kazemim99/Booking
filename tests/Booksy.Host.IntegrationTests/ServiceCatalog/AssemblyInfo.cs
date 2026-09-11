using Xunit;

// Updated 2026-09-11 for docs/TEST_ARCHITECTURE_AUDIT.md Phase 2. The two problems this comment
// used to describe are both fixed now: every test resets the database, the caches and every
// capturing fake before it runs (Booksy.Tests.Commons/DatabaseReset.cs, slice 1 — CleanDatabaseAsync
// really was a no-op, but it no longer exists), and all 33 host-booting classes here share one
// factory through BooksyHostTestCollection (slice 2, merged with UserManagement in slice 4), which
// is itself enough to serialise them
// against each other — xUnit never runs two classes in the same collection concurrently.
//
// This flag stays true for a different reason than the one FOLLOW-UPS #45 recorded: the handful of
// classes with no [Collection] at all — the plain unit tests under Unit/, no host, no database —
// each get their own default xUnit collection and WOULD run in parallel with the shared
// BooksyHostTestCollection and each other without this. That parallelism would be safe (nothing
// they touch overlaps), so removing this flag is a slice-5 task once the shared host's own
// parallelism (splitting BooksyHostTestCollection into several database-per-collection groups)
// is designed, not a one-line change to make today.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
