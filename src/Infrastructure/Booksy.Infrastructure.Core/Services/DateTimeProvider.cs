// ========================================
// Services/DateTimeProvider.cs
// ========================================
using Booksy.Core.Application.Abstractions.Services;

namespace Booksy.Infrastructure.Core.Services;

/// <summary>
/// Provides current date and time
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(DateTime.Now);



    public DateTime UtcToday => DateTime.UtcNow;

    // Returns current Unix timestamp in seconds
    public long UnixTimestamp =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // Returns current Unix timestamp in milliseconds
    public long UnixTimestampMilliseconds =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // Converts a Unix timestamp (seconds) to DateTime (UTC)
    public DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    // Converts DateTime to Unix timestamp (seconds)
    public long ToUnixTimestamp(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }
}

