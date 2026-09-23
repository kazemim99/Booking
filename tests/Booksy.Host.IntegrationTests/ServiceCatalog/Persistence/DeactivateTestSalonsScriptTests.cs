using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Npgsql;
using Xunit;
using Provider = Booksy.ServiceCatalog.Domain.Aggregates.Provider;
using ProviderStatus = Booksy.ServiceCatalog.Domain.Enums.ProviderStatus;

namespace Booksy.ServiceCatalog.IntegrationTests.Persistence;

/// <summary>
/// `deployment/sql/deactivate-test-salons.sql` is run by hand on production to take the two test salons out of
/// customer view (customer-app-ux-review-fixes, task I.1). It runs once, on live data, with no undo button, so this
/// runs the real file against the real schema: exactly those two rows change, a look-alike does not, and a second
/// run changes nothing.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class DeactivateTestSalonsScriptTests : ServiceCatalogIntegrationTestBase
{
    private static readonly Guid NotificationCheck = Guid.Parse("466e8bf3-c47d-417f-830d-9d0389f4eef6");
    private static readonly Guid AutomatedTest = Guid.Parse("9baeae5e-e9b5-4780-9816-16be959a02f0");
    private const string NotificationCheckName = "TEST notification check 6417131";
    private const string AutomatedTestName = "سالن تست خودکار";

    public DeactivateTestSalonsScriptTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static string ScriptText()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "deployment", "sql", "deactivate-test-salons.sql")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the script lives in the repository");
        return File.ReadAllText(Path.Combine(dir!.FullName, "deployment", "sql", "deactivate-test-salons.sql"));
    }

    /// <summary>Runs the file as psql would and returns the rows it reports as archived.</summary>
    private async Task<List<(Guid Id, string PreviousStatus)>> RunScriptAsync()
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(ScriptText(), connection);

        var archived = new List<(Guid, string)>();
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            if (!HasColumn(reader, "archived_salon_id")) continue;
            while (await reader.ReadAsync())
                archived.Add((reader.GetGuid(reader.GetOrdinal("archived_salon_id")),
                    reader.GetString(reader.GetOrdinal("previous_status"))));
        }
        while (await reader.NextResultAsync());
        return archived;
    }

    private static bool HasColumn(NpgsqlDataReader reader, string name) =>
        Enumerable.Range(0, reader.FieldCount).Any(i => reader.GetName(i) == name);

    private async Task<Dictionary<Guid, (string Status, int Version)>> StateAsync(params Guid[] ids)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """SELECT "Id", "Status", "Version" FROM "ServiceCatalog"."Providers" WHERE "Id" = ANY(@ids)""", connection);
        command.Parameters.AddWithValue("ids", ids);
        var state = new Dictionary<Guid, (string, int)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            state[reader.GetGuid(0)] = (reader.GetString(1), reader.GetInt32(2));
        return state;
    }

    private async Task<Guid> SeedSalonAsync(Guid id, string businessName, ProviderStatus status)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ServiceCategory.BeautySalon,
            ContactInfo.Create(Email.Create($"{Guid.NewGuid():N}@test.com"), PhoneNumber.From("+1234567890")),
            BusinessAddress.Create("123 Test St", "123 Test St", "Tehran", "Tehran", "12345", "Iran"));
        // Production's ids, so the file is exercised exactly as it will run on the box.
        provider.Id = ProviderId.From(id);
        provider.ClearDomainEvents();
        provider.SetSatus(status);
        await CreateEntityAsync(provider);
        return id;
    }

    [Fact]
    public async Task Archives_exactly_the_two_test_salons_and_a_second_run_changes_nothing()
    {
        await SeedSalonAsync(NotificationCheck, NotificationCheckName, ProviderStatus.Active);
        await SeedSalonAsync(AutomatedTest, AutomatedTestName, ProviderStatus.Drafted);
        // Look-alikes: the same Persian name on a real salon, and the next number along. Neither is a target.
        var sameName = await SeedSalonAsync(Guid.NewGuid(), AutomatedTestName, ProviderStatus.Drafted);
        var nextNumber = await SeedSalonAsync(Guid.NewGuid(), "TEST notification check 6417132", ProviderStatus.Active);
        var before = await StateAsync(sameName, nextNumber);

        var reported = await RunScriptAsync();

        reported.Should().BeEquivalentTo(new[] { (NotificationCheck, "Active"), (AutomatedTest, "Drafted") },
            "the script reports each salon it archived and the status it had, which is what the reverse restores");
        var after = await StateAsync(NotificationCheck, AutomatedTest, sameName, nextNumber);
        after[NotificationCheck].Status.Should().Be("Archived");
        after[AutomatedTest].Status.Should().Be("Archived");
        after[sameName].Should().Be(before[sameName], "matching the name alone is not enough — the id must match too");
        after[nextNumber].Should().Be(before[nextNumber]);

        var secondRun = await RunScriptAsync();

        secondRun.Should().BeEmpty("an already-archived salon is left alone, so a re-run is harmless");
        (await StateAsync(NotificationCheck, AutomatedTest, sameName, nextNumber)).Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task Leaves_a_salon_alone_when_the_id_matches_but_the_name_does_not()
    {
        // If the salon was renamed into something real since the decision was taken, it is not ours to hide.
        await SeedSalonAsync(NotificationCheck, "سالن زیبایی نگار", ProviderStatus.Active);

        var reported = await RunScriptAsync();

        reported.Should().BeEmpty();
        (await StateAsync(NotificationCheck))[NotificationCheck].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Hides_the_salons_from_customer_search()
    {
        await SeedSalonAsync(NotificationCheck, NotificationCheckName, ProviderStatus.Active);

        await RunScriptAsync();

        var response = await Client.GetAsync("/api/v1/providers/search?searchTerm=6417131&pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        body.Should().NotContain(NotificationCheck.ToString());
    }
}
