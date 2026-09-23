using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Npgsql;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Persistence;

/// <summary>
/// `deployment/sql/suspect-shifted-bookings.sql` is run by hand on production, where a wrong column name would
/// fail at the worst moment. This runs the real file against the real schema (QA walkthrough 2026-09-22, task 3.0).
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class SuspectShiftedBookingsQueryTests : ServiceCatalogIntegrationTestBase
{
    public SuspectShiftedBookingsQueryTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static string QueryText()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "deployment", "sql", "suspect-shifted-bookings.sql")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the query lives in the repository");
        return File.ReadAllText(Path.Combine(dir!.FullName, "deployment", "sql", "suspect-shifted-bookings.sql"));
    }

    private async Task<List<Guid>> RunAsync(DateTimeOffset fixDeployedAt)
    {
        // psql substitutes :fix_deployed_at; here it is bound as a parameter instead.
        var sql = QueryText().Replace(":fix_deployed_at::timestamptz", "@fix_deployed_at");
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("fix_deployed_at", fixDeployedAt);

        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            ids.Add(reader.GetGuid(0));
        return ids;
    }

    private static DateTime Weekday(int daysAhead, int hour)
    {
        var day = DateTime.UtcNow.Date.AddDays(daysAhead);
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday) day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private static Guid IdOf(string body)
    {
        var root = JObject.Parse(body);
        var data = root["data"] as JObject ?? root;
        return Guid.Parse((data["bookingId"] ?? data["id"])!.Value<string>()!);
    }

    [Fact]
    public async Task Lists_a_customers_online_booking_and_never_a_salon_entered_one()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var online = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = Weekday(2, 10),
        });
        online.StatusCode.Should().Be(HttpStatusCode.Created, await online.Content.ReadAsStringAsync());
        var onlineId = IdOf(await online.Content.ReadAsStringAsync());

        AuthenticateAsProviderOwner(provider);
        var walkIn = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = Weekday(3, 12),
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = $"+98912{Random.Shared.Next(1000000, 9999999)}",
            notifyCustomer = false,
        });
        walkIn.StatusCode.Should().Be(HttpStatusCode.Created, await walkIn.Content.ReadAsStringAsync());
        var walkInId = IdOf(await walkIn.Content.ReadAsStringAsync());

        var suspects = await RunAsync(DateTimeOffset.UtcNow.AddMinutes(5));

        suspects.Should().Contain(onlineId);
        suspects.Should().NotContain(walkInId, "the salon's own entries always carried the right time");
    }

    [Fact]
    public async Task A_booking_made_after_the_fix_is_not_a_suspect()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = Weekday(4, 11),
        });
        var id = IdOf(await response.Content.ReadAsStringAsync());

        var suspects = await RunAsync(DateTimeOffset.UtcNow.AddHours(-1));

        suspects.Should().NotContain(id);
    }
}
