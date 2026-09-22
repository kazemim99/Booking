namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots;

/// <summary>
/// Why a day has no bookable slot, in words the provider can act on.
///
/// <para>Every empty day used to say «no qualified staff member is available». A provider whose
/// Sunday was set to 11:00–12:00 was told that for a 90-minute service, though the staff member was
/// qualified — the day was simply shorter than the service (production, 2026-09-19).</para>
/// </summary>
public static class EmptyDayReason
{
    public const string NoQualifiedStaff = "متأسفانه هیچ کارمند واجد شرایطی برای این سرویس در دسترس نیست.";
    public const string Closed = "مجموعه در این روز تعطیل است.";

    /// <param name="open">The day's opening time, or null when closed.</param>
    /// <param name="close">The day's closing time, or null when closed.</param>
    /// <param name="serviceMinutes">The whole visit's length.</param>
    public static string Describe(TimeOnly? open, TimeOnly? close, int serviceMinutes)
    {
        if (open is null || close is null)
        {
            return Closed;
        }

        var dayMinutes = (close.Value - open.Value).TotalMinutes;
        if (dayMinutes < serviceMinutes)
        {
            // Both sides read this: the salon in its own app and, since 2026-09-22, the customer in the empty day.
            // So it says what is true, and asks for nothing only a salon could do.
            return $"ساعات کاری این روز ({open.Value.ToString("HH:mm")} تا {close.Value.ToString("HH:mm")}) برای این خدمت " +
                   $"({serviceMinutes} دقیقه) کافی نیست. لطفاً روز دیگری را انتخاب کنید.";
        }

        return NoQualifiedStaff;
    }
}
