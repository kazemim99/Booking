namespace Booksy.ServiceCatalog.Domain.Policies;

/// <summary>
/// How far ahead a CUSTOMER may book (QA walkthrough 2026-09-22: "nobody should be able to book more than a week
/// out; the calendar should not even offer those days").
/// </summary>
/// <remarks>
/// <para>A salon booking into its own book is not limited by this — it fills its diary as far ahead as it likes.
/// This is the public booking window only.</para>
///
/// <para>The tightest rule wins: a service or a salon that sets a shorter window keeps it. A longer one is
/// capped, which is what makes the platform rule true for salons whose stored policy still says 90 days.</para>
/// </remarks>
public static class BookingHorizonPolicy
{
    /// <summary>The platform's public booking window, in days.</summary>
    public const int PlatformMaxAdvanceDays = 7;

    /// <summary>The window that actually applies, given whatever the service and the salon set.</summary>
    public static int EffectiveMaxAdvanceDays(int? serviceDays = null, int? providerDays = null)
    {
        var effective = PlatformMaxAdvanceDays;
        if (serviceDays is { } s && s > 0 && s < effective) effective = s;
        if (providerDays is { } p && p > 0 && p < effective) effective = p;
        return effective;
    }

    /// <summary>True when <paramref name="date"/> is further ahead than a customer may book.</summary>
    public static bool IsBeyondWindow(DateTime date, DateTime today, int? serviceDays = null, int? providerDays = null) =>
        (date.Date - today.Date).TotalDays > EffectiveMaxAdvanceDays(serviceDays, providerDays);

    /// <summary>What a customer is told when they pick a day past the window.</summary>
    public static string BeyondWindowMessage(int? serviceDays = null, int? providerDays = null) =>
        $"رزرو بیش از {EffectiveMaxAdvanceDays(serviceDays, providerDays)} روز آینده امکان‌پذیر نیست.";
}
