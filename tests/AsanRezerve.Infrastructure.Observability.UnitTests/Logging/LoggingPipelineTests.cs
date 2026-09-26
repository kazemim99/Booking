using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.Logging;

/// <summary>
/// <c>Logging:LogLevel</c> in the host's appsettings.json is what decides what gets logged (system-logging:
/// "Configured log levels are enforced per category").
/// <para>It was dead configuration: the host ran Serilog through <c>UseSerilog</c>, which bypasses those filters,
/// with no Serilog levels of its own — so production logged every EF Core SQL command at Information.</para>
/// </summary>
public sealed class LoggingPipelineTests : IDisposable
{
    private readonly LoggingHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public void EfCore_sql_is_not_logged_at_information_in_production()
    {
        var sql = _harness.Factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");

        sql.IsEnabled(LogLevel.Information).Should().BeFalse();
        sql.IsEnabled(LogLevel.Warning).Should().BeTrue("a failing command must still be seen");
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware")]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics")]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler")]
    [InlineData("DotNetCore.CAP.Internal.ConsumerRegister")]
    [InlineData("Npgsql.Command")]
    public void Framework_chatter_is_kept_at_warning(string category)
    {
        _harness.Factory.CreateLogger(category).IsEnabled(LogLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void Application_categories_log_at_information_but_not_debug()
    {
        var logger = _harness.Factory.CreateLogger("AsanRezerve.ServiceCatalog.Application.Commands.CreateBooking");

        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }

    [Fact]
    public void Startup_messages_stay_visible()
    {
        _harness.Factory.CreateLogger("Microsoft.Hosting.Lifetime").IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void An_enabled_event_reaches_the_sinks_masked_and_with_its_category()
    {
        _harness.Factory.CreateLogger("AsanRezerve.Test").LogWarning("Login failed for {PhoneNumber} with {Password}", "09121234567", "x");

        var logEvent = _harness.Sink.Single();
        logEvent.Level.Should().Be(LogEventLevel.Warning);
        ((ScalarValue)logEvent.Properties["SourceContext"]).Value.Should().Be("AsanRezerve.Test");
        logEvent.RenderMessage().Should().Be("Login failed for \"0912*****67\" with \"***\"");
    }

    [Fact]
    public void A_filtered_event_never_reaches_the_sinks()
    {
        _harness.Factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").LogInformation("SELECT 1");

        _harness.Sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void Every_event_of_a_request_carries_its_trace_id()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        _harness.Factory.CreateLogger("AsanRezerve.Test").LogInformation("inside the request");

        _harness.Sink.Single().TraceId.Should().Be(activity.TraceId);
    }

    [Fact]
    public void Scope_values_become_event_properties()
    {
        var logger = _harness.Factory.CreateLogger("AsanRezerve.Test");

        using (logger.BeginScope(new Dictionary<string, object?> { ["RequestPath"] = "/api/v1/providers" }))
        {
            logger.LogInformation("inside");
        }

        ((ScalarValue)_harness.Sink.Single().Properties["RequestPath"]).Value.Should().Be("/api/v1/providers");
    }
}
