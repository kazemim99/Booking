using AsanRezerve.Infrastructure.Observability.LogStore;
using FluentAssertions;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.LogStore;

/// <summary>
/// Which daily partitions of <c>observability.log_events</c> to create and which to drop (log-explorer: "Stored logs
/// are kept for 14 days"). Days are UTC; a day is dropped only once all of it is older than the retention, so no
/// event younger than the retention is ever lost.
/// </summary>
public sealed class LogPartitionPlanTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_partition_is_named_after_its_UTC_day()
    {
        LogPartitionPlan.NameOf(new DateOnly(2026, 9, 26)).Should().Be("log_events_p20260926");
    }

    [Fact]
    public void A_partition_covers_one_UTC_day()
    {
        var (from, to) = LogPartitionPlan.Bounds(new DateOnly(2026, 9, 26));

        from.Should().Be(new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero));
        to.Should().Be(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Yesterday_today_and_the_next_two_days_are_created_when_none_exist()
    {
        LogPartitionPlan.ToCreate(Now, []).Should().Equal(
            new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28));
    }

    [Fact]
    public void Existing_partitions_are_not_created_again()
    {
        LogPartitionPlan.ToCreate(Now, ["log_events_p20260925", "log_events_p20260926", "log_events_default"])
            .Should().Equal(new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28));
    }

    [Fact]
    public void Today_is_the_UTC_day_even_when_the_clock_has_an_offset()
    {
        // 02:00 on the 27th in Tehran is still the 26th in UTC.
        var tehran = new DateTimeOffset(2026, 9, 27, 2, 0, 0, TimeSpan.FromHours(3.5));

        LogPartitionPlan.ToCreate(tehran, []).Should().StartWith(new DateOnly(2026, 9, 25));
    }

    [Fact]
    public void Days_entirely_older_than_the_retention_are_dropped()
    {
        // Cutoff: 2026-09-12 10:30. The 11th ended before it; the 12th did not (it still holds events younger than 14 days).
        var existing = new[] { "log_events_p20260910", "log_events_p20260911", "log_events_p20260912", "log_events_p20260926" };

        LogPartitionPlan.ToDrop(Now, existing, retentionDays: 14)
            .Should().Equal("log_events_p20260910", "log_events_p20260911");
    }

    [Fact]
    public void Tables_that_are_not_daily_partitions_are_never_dropped()
    {
        var existing = new[] { "log_events_default", "log_events_p2026091", "log_events_pxxxxxxxx", "log_level_overrides", "log_events_p20260231" };

        LogPartitionPlan.ToDrop(Now, existing, retentionDays: 14).Should().BeEmpty();
    }

    [Fact]
    public void A_retention_below_one_day_is_treated_as_one_day()
    {
        LogPartitionPlan.ToDrop(Now, ["log_events_p20260924", "log_events_p20260925"], retentionDays: 0)
            .Should().Equal("log_events_p20260924");
    }
}
