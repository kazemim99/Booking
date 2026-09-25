using AsanRezerve.Infrastructure.Core.Persistence.Converters;
using FluentAssertions;

namespace AsanRezerve.Infrastructure.Core.UnitTests.Persistence;

/// <summary>
/// A booking's time is the salon's wall clock — «۱۰:۳۰» is half past ten at the salon, in no zone (FOLLOW-UPS #63).
/// Postgres still needs a UTC-marked value for its <c>timestamp with time zone</c> column, so the digits are stored
/// under that marker, unchanged; they must come back with NO zone, or every client moves them to its own time zone
/// (QA 2026-09-23: 10:30 displayed as 14:00 in Tehran).
/// </summary>
public class WallClockDateTimeConverterTests
{
    private static readonly WallClockDateTimeConverter Converter = new();

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void The_salons_digits_are_stored_unchanged(DateTimeKind kind)
    {
        var halfPastTen = new DateTime(2026, 9, 24, 10, 30, 0, kind);

        var stored = (DateTime)Converter.ConvertToProvider(halfPastTen)!;

        stored.Should().Be(new DateTime(2026, 9, 24, 10, 30, 0));
        stored.Kind.Should().Be(DateTimeKind.Utc, "Npgsql writes only a UTC-marked value to timestamptz");
    }

    [Fact]
    public void A_stored_time_comes_back_as_a_wall_clock_with_no_zone()
    {
        var fromDatabase = new DateTime(2026, 9, 24, 10, 30, 0, DateTimeKind.Utc);

        var read = (DateTime)Converter.ConvertFromProvider(fromDatabase)!;

        read.Should().Be(new DateTime(2026, 9, 24, 10, 30, 0));
        read.Kind.Should().Be(DateTimeKind.Unspecified,
            "serialized, an Unspecified DateTime carries no Z and no offset, so no client shifts it");
    }

    [Fact]
    public void Written_as_json_it_carries_no_zone()
    {
        var read = (DateTime)Converter.ConvertFromProvider(new DateTime(2026, 9, 24, 10, 30, 0, DateTimeKind.Utc))!;

        System.Text.Json.JsonSerializer.Serialize(read).Should().Be("\"2026-09-24T10:30:00\"");
    }
}
