using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Policies;

/// <summary>
/// When a confirmed visit the salon never marked done — nor no-show — becomes done by itself, so its customer can
/// review it (openspec/changes/_inline/reviews-and-reschedule-round2 D3). Before, only the salon could complete a
/// booking, and a salon that never pressed «انجام شد» could never be reviewed.
/// </summary>
/// <remarks>
/// Twelve hours after the appointment's END: a salon's working day to record a no-show first. Booking times are the
/// salon's wall clock (<see cref="SalonTime"/>), so "now" is read on that clock too; UTC would run 3:30 early.
/// </remarks>
public static class BookingAutoCompletion
{
    /// <summary>How long after its end an unmarked confirmed booking waits before it completes by itself.</summary>
    public static readonly TimeSpan After = TimeSpan.FromHours(12);

    /// <summary>The same wait in whole hours, in Persian digits, for the customer's copy («۱۲»).</summary>
    public static string AfterHoursInPersian =>
        string.Concat(((int)After.TotalHours).ToString(System.Globalization.CultureInfo.InvariantCulture)
            .Select(c => char.IsAsciiDigit(c) ? (char)('۰' + (c - '0')) : c));

    /// <summary>The latest salon-clock end time that is due at <paramref name="utcNow"/>: every end at or before it is.</summary>
    public static DateTime LatestDueEnd(DateTime utcNow) => SalonTime.FromUtc(utcNow) - After;

    /// <summary>Whether a booking ending at <paramref name="salonEnd"/> (salon clock) is due at <paramref name="utcNow"/>.</summary>
    public static bool IsDue(DateTime salonEnd, DateTime utcNow) => salonEnd <= LatestDueEnd(utcNow);
}
