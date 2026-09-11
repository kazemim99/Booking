using Booksy.Infrastructure.Core.Persistence.Converters;
using FluentAssertions;

namespace Booksy.Infrastructure.Core.UnitTests.Persistence;

/// <summary>
/// Every DateTime in this system is a UTC instant, and Postgres only accepts one marked as such for a
/// <c>timestamp with time zone</c> column. The converter is where values without that marker — a query
/// string's <c>?date=2026-09-14</c>, a <c>DateOnly.ToDateTime(...)</c> — get their meaning. Getting it
/// wrong shifts every such instant by the machine's UTC offset, which is FOLLOW-UPS #48.
/// </summary>
public class UtcDateTimeConverterTests
{
    private static readonly DateTime Instant = new(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_Utc_Value_Is_Stored_Unchanged()
    {
        UtcDateTimeConverter.ToUtc(Instant).Should().Be(Instant)
            .And.Subject.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void An_Unspecified_Value_Is_Taken_To_Be_Utc_Not_Local()
    {
        var naive = DateTime.SpecifyKind(Instant, DateTimeKind.Unspecified);

        var stored = UtcDateTimeConverter.ToUtc(naive);

        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(Instant,
            "reading it as the server's local time is exactly the shift the legacy Npgsql switch applied");
    }

    [Fact]
    public void A_Local_Value_Is_Converted_To_The_Same_Instant()
    {
        var local = Instant.ToLocalTime();

        var stored = UtcDateTimeConverter.ToUtc(local);

        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(Instant);
    }

    [Fact]
    public void A_Value_Read_Back_Is_Marked_Utc()
    {
        var converter = new UtcDateTimeConverter();
        var fromDatabase = DateTime.SpecifyKind(Instant, DateTimeKind.Unspecified);

        var read = (DateTime)converter.ConvertFromProvider(fromDatabase)!;

        read.Kind.Should().Be(DateTimeKind.Utc,
            "a value compared against DateTime.UtcNow must be in the same frame as it");
        read.Should().Be(Instant);
    }
}
