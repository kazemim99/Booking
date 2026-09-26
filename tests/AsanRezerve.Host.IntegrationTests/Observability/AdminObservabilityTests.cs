using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.Infrastructure.Observability.LogStore;
using AsanRezerve.ServiceCatalog.IntegrationTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.Host.IntegrationTests.Observability;

/// <summary>
/// The admin observability API against the real composed host and Postgres (log-explorer, runtime-log-levels,
/// read-caching: admin endpoints). Events are written with the host's own logger and read back through HTTP, so
/// the whole path — masking, the log store writer, the migration, the queries, AdminOnly — is exercised.
/// Each test marks its events with a unique token: the log table is shared by every test and never truncated.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class AdminObservabilityTests : ServiceCatalogIntegrationTestBase
{
    private const string Base = "/api/v1/admin/observability";

    public AdminObservabilityTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private ILogger HostLogger(string category = "AsanRezerve.Tests.Observability") =>
        Factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category);

    private Task FlushLogStoreAsync() =>
        Factory.Services.GetRequiredService<LogStoreWriter>().FlushAsync(TimeSpan.FromSeconds(15));

    private async Task<JToken> DataAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JObject.Parse(text)["data"]!;
    }

    private static string Marker() => "obs-" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Logs_are_for_admins_only()
    {
        ClearAuthentication();
        (await Client.GetAsync($"{Base}/logs")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        AuthenticateAsCustomer(Guid.NewGuid());
        (await Client.GetAsync($"{Base}/logs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync($"{Base}/log-levels")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync($"{Base}/cache/invalidate", new { all = true })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        AuthenticateAsAdmin();
        (await Client.GetAsync($"{Base}/logs")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_logged_event_is_found_by_text_and_level_masked()
    {
        AuthenticateAsAdmin();
        var marker = Marker();
        HostLogger().LogWarning("Payment retry {Marker} for {PhoneNumber}", marker, "09121234567");
        HostLogger().LogInformation("Below the filter {Marker}", marker); // Testing logs AsanRezerve.* from Warning up
        await FlushLogStoreAsync();

        var page = await DataAsync(await Client.GetAsync($"{Base}/logs?search={marker}&minLevel=Warning"));

        page["totalCount"]!.Value<long>().Should().Be(1);
        var item = page["items"]![0]!;
        item["level"]!.Value<string>().Should().Be("Warning");
        item["message"]!.Value<string>().Should().Contain("0912*****67").And.NotContain("09121234567");
        item["sourceContext"]!.Value<string>().Should().Be("AsanRezerve.Tests.Observability");
    }

    [Fact]
    public async Task One_request_can_be_followed_by_its_trace_id()
    {
        AuthenticateAsAdmin();
        var marker = Marker();
        string traceId;
        using (var activity = new Activity("simulated-request").SetIdFormat(ActivityIdFormat.W3C).Start())
        {
            traceId = activity.TraceId.ToHexString();
            HostLogger().LogWarning("Step one {Marker} {BookingId}", marker, "b-1");
            HostLogger().LogError(new InvalidOperationException("boom"), "Step two {Marker}", marker);
        }
        await FlushLogStoreAsync();

        var trace = (JArray)await DataAsync(await Client.GetAsync($"{Base}/logs/trace/{traceId}"));

        trace.Select(e => e["message"]!.Value<string>()).Should().HaveCount(2)
            .And.SatisfyRespectively(
                first => first.Should().StartWith("Step one"),
                second => second.Should().StartWith("Step two"));

        var detail = await DataAsync(await Client.GetAsync($"{Base}/logs/{trace[0]!["id"]!.Value<long>()}"));
        detail["properties"]!["BookingId"]!.Value<string>().Should().Be("b-1");
        trace[1]!["exception"]!.Value<string>().Should().Contain("InvalidOperationException: boom");
    }

    [Fact]
    public async Task Every_response_carries_the_trace_id_its_envelope_reports()
    {
        AuthenticateAsAdmin();

        var response = await Client.GetAsync($"{Base}/log-levels");

        var header = response.Headers.GetValues("X-Trace-Id").Single();
        header.Should().MatchRegex("^[0-9a-f]{32}$");
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        body["metadata"]!["traceId"]!.Value<string>().Should().Be(header);
    }

    [Fact]
    public async Task An_admin_can_change_a_level_and_reset_it()
    {
        AuthenticateAsAdmin();
        var category = "AsanRezerve.Tests.Probe" + Guid.NewGuid().ToString("N")[..8];
        var probe = HostLogger(category + ".Handler");
        probe.IsEnabled(LogLevel.Debug).Should().BeFalse();

        var set = await Client.PutAsJsonAsync($"{Base}/log-levels", new { category, level = "Debug", durationMinutes = 30 });
        var row = await DataAsync(set);
        row["effectiveLevel"]!.Value<string>().Should().Be("Debug");
        row["override"]!["expiresAt"]!.Type.Should().NotBe(JTokenType.Null);
        probe.IsEnabled(LogLevel.Debug).Should().BeTrue("the change applies to loggers that already exist");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>();
            (await db.LogLevelOverrides.AnyAsync(o => o.Category == category)).Should().BeTrue("it must survive a restart");
        }

        var levels = (JArray)await DataAsync(await Client.GetAsync($"{Base}/log-levels"));
        levels.Should().Contain(l => l["category"]!.Value<string>() == category);

        (await Client.DeleteAsync($"{Base}/log-levels/{category}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        probe.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }

    [Theory]
    [InlineData("Bad Category", "Debug")]
    [InlineData("AsanRezerve.Tests", "Loud")]
    public async Task Invalid_level_changes_are_rejected(string category, string level)
    {
        AuthenticateAsAdmin();

        var response = await Client.PutAsJsonAsync($"{Base}/log-levels", new { category, level });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_cache_page_shows_hits_and_a_purge_empties_a_salon()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}");
        await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}");
        AuthenticateAsAdmin();

        var cache = await DataAsync(await Client.GetAsync($"{Base}/cache"));
        ((JArray)cache["regions"]!).Should().Contain(r => r["region"]!.Value<string>() == "GetProviderByIdQuery");
        cache["l2"]!["circuit"]!.Value<string>().Should().Be("Closed");

        var metrics = Factory.Services.GetRequiredService<CacheMetrics>();
        var missesBefore = metrics.Snapshot().Single(r => r.Region == "GetProviderByIdQuery").Misses;
        var purge = await Client.PostAsJsonAsync($"{Base}/cache/invalidate", new { tag = $"provider:{provider.Id.Value}" });
        purge.StatusCode.Should().Be(HttpStatusCode.NoContent, await purge.Content.ReadAsStringAsync());

        await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}");
        metrics.Snapshot().Single(r => r.Region == "GetProviderByIdQuery").Misses.Should().Be(missesBefore + 1);
    }

    [Fact]
    public async Task The_digest_groups_repeated_errors_for_an_ai_reader()
    {
        AuthenticateAsAdmin();
        var marker = Marker();
        for (var i = 0; i < 3; i++)
        {
            using var activity = new Activity("r").SetIdFormat(ActivityIdFormat.W3C).Start();
            HostLogger("AsanRezerve.Tests.Digest." + marker).LogError(new TimeoutException("gateway slow"), "Gateway call failed {Attempt}", i);
        }
        await FlushLogStoreAsync();

        // Narrowed to this test's source: the whole suite logs errors into the same table, and the digest lists the
        // top groups only.
        var source = "AsanRezerve.Tests.Digest." + marker;
        var json = await DataAsync(await Client.GetAsync($"{Base}/digest?format=json&source={source}"));
        var group = ((JArray)json["errorGroups"]!).Single(g => g["sourceContext"]!.Value<string>() == "AsanRezerve.Tests.Digest." + marker);
        group["count"]!.Value<long>().Should().Be(3);
        ((JArray)group["sampleTraceIds"]!).Should().NotBeEmpty();
        group["exceptionType"]!.Value<string>().Should().StartWith("System.TimeoutException");

        var markdown = (await DataAsync(await Client.GetAsync($"{Base}/digest?format=markdown&source={source}")))["markdown"]!.Value<string>();
        markdown.Should().Contain("# AsanRezerve system digest").And.Contain("AsanRezerve.Tests.Digest." + marker);
    }

    [Fact]
    public async Task The_overview_reports_process_cache_and_log_store_health()
    {
        AuthenticateAsAdmin();

        var overview = await DataAsync(await Client.GetAsync($"{Base}/overview"));

        overview["process"]!["workingSetMb"]!.Value<double>().Should().BeGreaterThan(0);
        overview["logStore"]!["ready"]!.Value<bool>().Should().BeTrue();
        overview["cache"]!["l2"].Should().NotBeNull();
        overview["environment"]!.Value<string>().Should().Be("Testing");
    }

    [Fact]
    public async Task An_export_is_newline_delimited_json_not_an_envelope()
    {
        AuthenticateAsAdmin();
        var marker = Marker();
        HostLogger().LogWarning("Export me {Marker}", marker);
        HostLogger().LogWarning("And me {Marker}", marker);
        await FlushLogStoreAsync();

        var response = await Client.GetAsync($"{Base}/logs/export?search={marker}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/x-ndjson");
        var lines = (await response.Content.ReadAsStringAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines.Select(l => JObject.Parse(l)["message"]!.Value<string>()).Should().OnlyContain(m => m.Contains(marker));
    }

    [Fact]
    public async Task Events_older_than_the_retention_are_deleted()
    {
        var marker = Marker();
        var writer = Factory.Services.GetRequiredService<ILogEventBatchWriter>();
        var now = DateTimeOffset.UtcNow;
        LogEventRow Row(DateTimeOffset at, string text) =>
            new(at, 3, text, "x", null, "AsanRezerve.Tests.Retention", null, null, null, null, null, null, null, null);
        await writer.WriteAsync([Row(now.AddDays(-15), "old " + marker), Row(now.AddDays(-1), "recent " + marker)], CancellationToken.None);

        await Factory.Services.GetRequiredService<LogRetention>().DeleteOlderThanAsync(now.AddDays(-14));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>();
        var left = await db.LogEvents.Where(e => e.Message.EndsWith(marker)).Select(e => e.Message).ToListAsync();
        left.Should().Equal("recent " + marker);
    }
}
