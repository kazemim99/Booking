using Booksy.Tests.Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Proves the acceptance scenario of docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 1 (S2): every
/// test starts from an empty database. Read it in the order xUnit actually runs a class in: this
/// project has ~40 other classes ahead of and behind this one in the same collection, all writing
/// rows, and this test's own <c>InitializeAsync</c> ran the same reset every other test's did — so
/// if any table here is non-empty, the reset is not doing its job for the WHOLE suite, not just
/// this class.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class DatabaseResetSelfTests : ServiceCatalogIntegrationTestBase
{
    public DatabaseResetSelfTests(BooksyHostFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Every_managed_table_is_empty_at_the_start_of_a_test()
    {
        // Without this the check below passes vacuously: an empty table list has no non-empty tables.
        // That is exactly how a reset that truncated nothing went unnoticed.
        var managed = await Factory.DatabaseReset.GetManagedTablesAsync();
        managed.Should().Contain("\"ServiceCatalog\".\"Providers\"",
            "the reset must know about the tables it is supposed to empty");

        var nonEmpty = await Factory.DatabaseReset.GetNonEmptyTablesAsync();

        nonEmpty.Should().BeEmpty(
            "InitializeAsync must reset every table this fixture manages before the test body runs; " +
            "a non-empty table here means some earlier test's rows leaked into this one");
    }

    // The reset TRUNCATEs every table in one statement while the host's timer-driven services (the outbox sweep,
    // payment reconciliation, ledger maintenance) keep querying. When a tick lands on a reset the two take table
    // locks in opposite orders and Postgres kills one as a deadlock (40P01) — any test, at random, green alone.
    // Four FULL runs were lost to it on 2026-09-22/23 before IncludeErrorDetail named it:
    //   "Process A waits for AccessExclusiveLock on relation …; blocked by process B.
    //    Process B waits for AccessShareLock on relation …; blocked by process A."
    // Postgres rolls back only the victim, and truncating test data is idempotent, so the reset retries.

    [Fact]
    public async Task A_reset_that_loses_a_deadlock_is_retried()
    {
        var attempts = 0;

        await DatabaseReset.RetryingDeadlocksAsync(() =>
        {
            attempts++;
            if (attempts < 3) throw Deadlock();
            return Task.CompletedTask;
        });

        attempts.Should().Be(3);
    }

    [Fact]
    public async Task Only_a_deadlock_is_retried()
    {
        var attempts = 0;

        Func<Task> act = () => DatabaseReset.RetryingDeadlocksAsync(() =>
        {
            attempts++;
            throw new Npgsql.PostgresException("relation does not exist", "ERROR", "ERROR", "42P01");
        });

        await act.Should().ThrowAsync<Npgsql.PostgresException>();
        attempts.Should().Be(1, "any other failure is a real problem and must surface at once");
    }

    [Fact]
    public async Task A_deadlock_that_keeps_coming_back_is_not_hidden_forever()
    {
        var attempts = 0;

        Func<Task> act = () => DatabaseReset.RetryingDeadlocksAsync(() =>
        {
            attempts++;
            throw Deadlock();
        });

        await act.Should().ThrowAsync<Npgsql.PostgresException>();
        attempts.Should().Be(DatabaseReset.MaxDeadlockRetries + 1);
    }

    private static Npgsql.PostgresException Deadlock() =>
        new("deadlock detected", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.DeadlockDetected);
}
