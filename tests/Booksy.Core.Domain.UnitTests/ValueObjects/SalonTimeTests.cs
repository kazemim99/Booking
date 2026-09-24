using Booksy.Core.Domain.ValueObjects;
using FluentAssertions;

namespace Booksy.Core.Domain.UnitTests.ValueObjects;

/// <summary>
/// A booking's time is the salon's wall clock («۱۰:۰۰» is ten o'clock at the salon, in no zone). Asking "has it
/// started yet?" means reading the clock the salon reads — not the server's UTC clock, which in Iran is three and a
/// half hours behind. Comparing the two directly is what refused to mark a 10:00 appointment done at 10:33
/// (QA 2026-09-24).
/// </summary>
public sealed class SalonTimeTests
{
    [Fact]
    public void An_instant_reads_as_the_salons_clock()
    {
        var instant = new DateTime(2026, 9, 24, 7, 3, 0, DateTimeKind.Utc);

        var salon = SalonTime.FromUtc(instant);

        salon.Should().Be(new DateTime(2026, 9, 24, 10, 33, 0));
        salon.Kind.Should().Be(DateTimeKind.Unspecified, "a wall-clock value carries no zone");
    }

    [Fact]
    public void A_salon_time_converts_back_to_the_instant_it_names()
    {
        var salon = new DateTime(2026, 9, 25, 8, 0, 0);

        var instant = SalonTime.ToUtc(salon);

        instant.Should().Be(new DateTime(2026, 9, 25, 4, 30, 0));
        instant.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Just_after_midnight_at_the_salon_is_still_the_previous_day_in_utc()
    {
        var instant = new DateTime(2026, 9, 24, 21, 0, 0, DateTimeKind.Utc);

        SalonTime.FromUtc(instant).Date.Should().Be(new DateTime(2026, 9, 25));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void The_offset_is_the_same_in_every_season(int month)
    {
        // Iran stopped observing daylight saving time in 2022.
        var instant = new DateTime(2026, month, 15, 12, 0, 0, DateTimeKind.Utc);

        (SalonTime.FromUtc(instant) - DateTime.SpecifyKind(instant, DateTimeKind.Unspecified))
            .Should().Be(TimeSpan.FromMinutes(210));
    }

    [Fact]
    public void Now_is_the_utc_clock_read_at_the_salon()
    {
        var before = SalonTime.FromUtc(DateTime.UtcNow);
        var now = SalonTime.Now;
        var after = SalonTime.FromUtc(DateTime.UtcNow);

        now.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
