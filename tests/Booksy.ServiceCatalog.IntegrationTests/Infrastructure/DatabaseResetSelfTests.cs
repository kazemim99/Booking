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
[Collection(ServiceCatalogTestCollection.Name)]
public class DatabaseResetSelfTests : ServiceCatalogIntegrationTestBase
{
    public DatabaseResetSelfTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Every_managed_table_is_empty_at_the_start_of_a_test()
    {
        var nonEmpty = await Factory.DatabaseReset.GetNonEmptyTablesAsync();

        nonEmpty.Should().BeEmpty(
            "InitializeAsync must reset every table this fixture manages before the test body runs; " +
            "a non-empty table here means some earlier test's rows leaked into this one");
    }
}
