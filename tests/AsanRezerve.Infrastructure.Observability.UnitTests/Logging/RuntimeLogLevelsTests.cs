using AsanRezerve.Infrastructure.Observability.Logging.Levels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.Logging;

/// <summary>
/// Admins change log levels at runtime, per category, optionally for a limited time (runtime-log-levels).
/// <para>Overrides are keys of a top-precedence configuration provider (<c>Logging:LogLevel:&lt;category&gt;</c>);
/// Microsoft.Extensions.Logging re-applies its filter rules to every existing logger when it reloads — so a change
/// takes effect at once, for loggers created long before it, without a restart.</para>
/// </summary>
public sealed class RuntimeLogLevelsTests : IDisposable
{
    private readonly ManualTimeProvider _time = new();
    private readonly InMemoryLogLevelOverrideStore _store = new();
    private readonly LoggingHarness _harness;

    public RuntimeLogLevelsTests()
    {
        _harness = new LoggingHarness(time: _time, store: _store);
    }

    public void Dispose() => _harness.Dispose();

    private ILogger Logger(string category) => _harness.Factory.CreateLogger(category);

    [Fact]
    public async Task Debugging_one_namespace_changes_it_and_its_children_only()
    {
        var booking = Logger("AsanRezerve.ServiceCatalog.Application.Commands.CreateBooking"); // created before the change
        var users = Logger("AsanRezerve.UserManagement.Application.Commands.Login");

        await _harness.Levels.SetAsync("AsanRezerve.ServiceCatalog", LogLevel.Debug, duration: null, actor: "admin-1");

        booking.IsEnabled(LogLevel.Debug).Should().BeTrue();
        users.IsEnabled(LogLevel.Debug).Should().BeFalse();
        users.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public async Task Reset_returns_to_the_configured_level()
    {
        var logger = Logger("AsanRezerve.ServiceCatalog.X");
        await _harness.Levels.SetAsync("AsanRezerve.ServiceCatalog", LogLevel.Debug, null, "admin-1");

        await _harness.Levels.ResetAsync("AsanRezerve.ServiceCatalog", "admin-1");

        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public async Task A_temporary_override_reverts_by_itself()
    {
        var logger = Logger("AsanRezerve.ServiceCatalog.X");
        await _harness.Levels.SetAsync("AsanRezerve.ServiceCatalog", LogLevel.Debug, TimeSpan.FromMinutes(30), "admin-1");

        _time.Advance(TimeSpan.FromMinutes(29));
        await _harness.Levels.ExpireDueAsync();
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue("29 minutes of 30 have passed");

        _time.Advance(TimeSpan.FromMinutes(2));
        await _harness.Levels.ExpireDueAsync();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        _store.Items.Should().BeEmpty("an expired override is not re-applied after a restart either");
    }

    [Fact]
    public async Task The_default_level_can_be_changed()
    {
        var other = Logger("Some.Library.Without.Its.Own.Rule");

        await _harness.Levels.SetAsync(LogLevelService.DefaultCategory, LogLevel.Warning, null, "admin-1");

        other.IsEnabled(LogLevel.Information).Should().BeFalse();
    }

    [Theory]
    [InlineData("AsanRezerve.Service Catalog")]
    [InlineData("AsanRezerve;DROP")]
    [InlineData("")]
    [InlineData("Logging:LogLevel:X")]
    public async Task An_invalid_category_is_rejected(string category)
    {
        var act = () => _harness.Levels.SetAsync(category, LogLevel.Debug, null, "admin-1");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task A_duration_outside_the_allowed_window_is_rejected()
    {
        var act = () => _harness.Levels.SetAsync("AsanRezerve", LogLevel.Debug, TimeSpan.FromDays(30), "admin-1");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Every_change_is_audited()
    {
        await _harness.Levels.SetAsync("AsanRezerve.ServiceCatalog", LogLevel.Debug, TimeSpan.FromMinutes(30), "admin-1");

        var audit = _harness.Sink.Events.Should().ContainSingle(e => e.MessageTemplate.Text.Contains("Log level for")).Subject;
        audit.Level.Should().Be(LogEventLevel.Warning);
        ((ScalarValue)audit.Properties["Category"]).Value.Should().Be("AsanRezerve.ServiceCatalog");
        ((ScalarValue)audit.Properties["PreviousLevel"]).Value.Should().Be("Information");
        ((ScalarValue)audit.Properties["NewLevel"]).Value.Should().Be("Debug");
        ((ScalarValue)audit.Properties["Actor"]).Value.Should().Be("admin-1");
    }

    [Fact]
    public async Task The_list_shows_configured_levels_and_overrides()
    {
        await _harness.Levels.SetAsync("AsanRezerve.ServiceCatalog", LogLevel.Debug, TimeSpan.FromMinutes(30), "admin-1");

        var levels = _harness.Levels.GetLevels();

        var ef = levels.Single(l => l.Category == "Microsoft.EntityFrameworkCore");
        ef.ConfiguredLevel.Should().Be(LogLevel.Warning);
        ef.EffectiveLevel.Should().Be(LogLevel.Warning);
        ef.Override.Should().BeNull();

        var catalog = levels.Single(l => l.Category == "AsanRezerve.ServiceCatalog");
        catalog.ConfiguredLevel.Should().BeNull("appsettings has no rule of its own for it");
        catalog.EffectiveLevel.Should().Be(LogLevel.Debug);
        catalog.Override!.UpdatedBy.Should().Be("admin-1");
        catalog.Override.ExpiresAt.Should().Be(_time.GetUtcNow().AddMinutes(30));

        levels.Should().Contain(l => l.Category == LogLevelService.DefaultCategory && l.ConfiguredLevel == LogLevel.Information);
    }

    [Fact]
    public async Task Persisted_overrides_are_reapplied_at_startup_but_expired_ones_are_dropped()
    {
        await _store.UpsertAsync(new LogLevelOverride("AsanRezerve.ServiceCatalog", LogLevel.Debug, _time.GetUtcNow().AddMinutes(10), "admin-1", _time.GetUtcNow()));
        await _store.UpsertAsync(new LogLevelOverride("AsanRezerve.UserManagement", LogLevel.Trace, _time.GetUtcNow().AddMinutes(-1), "admin-1", _time.GetUtcNow()));

        await _harness.Levels.LoadAsync();

        Logger("AsanRezerve.ServiceCatalog.X").IsEnabled(LogLevel.Debug).Should().BeTrue();
        Logger("AsanRezerve.UserManagement.X").IsEnabled(LogLevel.Debug).Should().BeFalse();
        _store.Items.Select(o => o.Category).Should().Equal("AsanRezerve.ServiceCatalog");
    }
}
