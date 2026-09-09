using Xunit;

// One database, no isolation, so the suite must not race itself.
//
// Every class in this project talks to the SAME Testcontainers Postgres, and the shared base's
// CleanDatabaseAsync is a no-op. xUnit runs test classes in parallel by default, so classes were
// competing for one database and for the connection pool: two full runs on identical code gave 60
// and 61 failures with a DIFFERENT flaky set each time (FOLLOW-UPS #45), and the FULL verify has
// twice now failed on an "off-baseline" test that passes in isolation — most recently
// BookingSlotIntegrityTests' 40-way concurrent-insert stress test, which passes alone for all three
// seeds.
//
// A verification gate that is wrong several tests in either direction cannot tell a regression from
// a race, which is the one thing it exists to do. Serialising the assembly buys that back. It costs
// wall-clock time on the db:Booksy.ServiceCatalog.IntegrationTests step; the honest per-class
// isolation (a schema or database per class) is the real fix and stays open under #45.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
