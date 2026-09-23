using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Booksy.Infrastructure.Core.Persistence.Converters;

/// <summary>
/// Enforces this system's rule that every <see cref="DateTime"/> is a UTC instant, at the one place
/// every value passes through on its way to Postgres — including query parameters, which EF Core
/// converts with the converter of the column they are compared against.
///
/// <para><b>Why it exists.</b> Every timestamp column is <c>timestamp with time zone</c>, and modern
/// Npgsql refuses to write a <see cref="DateTime"/> to one unless its <c>Kind</c> is <c>Utc</c>.
/// Plenty of legitimate values arrive without that marker: a query string's <c>?date=2026-09-14</c>
/// binds as <c>Kind=Unspecified</c>, and so does <c>DateOnly.ToDateTime(...)</c>. For years the hosts
/// ran with <c>Npgsql.EnableLegacyTimestampBehavior</c>, which accepted those values by treating them
/// as the server's LOCAL time — shifting each one by the machine's UTC offset — and read every value
/// back as <c>Kind=Local</c>, which put every comparison against <c>DateTime.UtcNow</c> off by the same
/// offset (FOLLOW-UPS #48).</para>
///
/// <para><b>The rule.</b> <c>Utc</c> is stored as is. <c>Local</c> is converted, which loses nothing.
/// <c>Unspecified</c> is taken to be UTC — the reading every consumer in this codebase already
/// assumes (see <c>AvailabilityService</c>, which did exactly this by hand), and the only one
/// consistent with the invariant. Values come back <c>Kind=Utc</c>.</para>
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => ToUtc(v), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }

    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

/// <summary>
/// For the one kind of <see cref="DateTime"/> that is NOT an instant: a booking's time, which is the salon's wall
/// clock («۱۰:۳۰» is half past ten at the salon, in no zone — FOLLOW-UPS #63).
///
/// <para>It is stored exactly as <see cref="UtcDateTimeConverter"/> stores it (the digits under a UTC marker, which is
/// what Postgres needs for <c>timestamp with time zone</c>), so storage, queries and every existing row are unchanged.
/// It is read back <c>Kind=Unspecified</c> instead of <c>Utc</c>: serialized, that has no <c>Z</c> and no offset, so a
/// client reads the clock it is. Read back as UTC, the API wrote "…T10:30:00Z" and every client (Flutter's
/// <c>toLocal()</c>, the browser's <c>new Date(…)</c>) moved it to its own zone — QA 2026-09-23: a 10:30 booking
/// showed as 14:00 on the customer's appointment and on the salon's calendar.</para>
///
/// <para>Comparisons are unaffected: .NET compares DateTimes by their digits, whatever their Kind.</para>
/// </summary>
public sealed class WallClockDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public WallClockDateTimeConverter()
        : base(
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified))
    {
    }
}

public static class UtcDateTimeConventions
{
    /// <summary>
    /// Applies <see cref="UtcDateTimeConverter"/> to every <see cref="DateTime"/> and nullable
    /// <see cref="DateTime"/> property in the model. Call from <c>ConfigureConventions</c>.
    /// </summary>
    public static ModelConfigurationBuilder UseUtcDateTimes(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        return configurationBuilder;
    }
}
