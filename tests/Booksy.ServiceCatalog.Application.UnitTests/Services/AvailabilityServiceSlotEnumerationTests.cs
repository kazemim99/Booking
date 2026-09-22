using Booksy.ServiceCatalog.Application.Services;

namespace Booksy.ServiceCatalog.Application.UnitTests.Services;

/// <summary>
/// Regression tests for the slot-start enumeration extracted from
/// AvailabilityService. The original implementation marched a TimeOnly with
/// AddMinutes, which wraps past midnight — a 00:00–23:59 schedule (24h
/// business) made the loop condition stay true and hung the request thread
/// forever (found via live E2E, 2026-07-15).
/// </summary>
public class AvailabilityServiceSlotEnumerationTests
{
    [Fact]
    public void Terminates_For_A_Full_Day_Schedule_That_Previously_Hung()
    {
        // 00:00–23:59, 45-minute service, 30-minute interval.
        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(0, 0), new TimeOnly(23, 59), 45, 30).ToList();

        // Last start: 23:00 (1380) since 23:00+45 = 23:45 <= 23:59,
        // and 23:30+45 = 24:15 exceeds the day.
        Assert.Equal(0, slots.First());
        Assert.Equal(1380, slots.Last());
        Assert.Equal(47, slots.Count);
    }

    [Fact]
    public void Generates_Expected_Slots_For_Business_Hours()
    {
        // 09:00–18:00, 45-minute service, 30-minute interval.
        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30).ToList();

        Assert.Equal(9 * 60, slots.First());
        Assert.Equal(17 * 60 + 0, slots.Last()); // 17:00 + 45 = 17:45 <= 18:00
        Assert.Equal(17, slots.Count);
    }

    [Fact]
    public void Slot_Near_Closing_Fits_Exactly()
    {
        // 23:00–23:59, 45-minute service: only 23:00 fits.
        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(23, 0), new TimeOnly(23, 59), 45, 30).ToList();

        Assert.Single(slots);
        Assert.Equal(23 * 60, slots[0]);
    }

    [Fact]
    public void Empty_When_Window_Cannot_Fit_The_Service()
    {
        Assert.Empty(AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(10, 0), new TimeOnly(10, 30), 45, 30));
        Assert.Empty(AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(10, 0), new TimeOnly(10, 0), 45, 30));
    }

    [Fact]
    public void Degenerate_Inputs_Yield_Nothing_Instead_Of_Looping()
    {
        Assert.Empty(AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 0, 30));
        Assert.Empty(AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 0));
    }

    // QA walkthrough 2026-09-22: "a 45-minute booking at 14:00 must make the next slot 14:45, not 14:30 — the
    // times after a booking follow from when it ends". A fixed grid alone cannot express that: it offered 15:00
    // (the first grid mark clear of the booking) and left the quarter hour in between unsellable.

    [Fact]
    public void A_booking_makes_the_next_start_the_moment_it_ends()
    {
        // 09:00–18:00, 45-minute service on a 30-minute grid, with 14:00–14:45 taken.
        var busy = new[] { (14 * 60, 14 * 60 + 45) };

        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30, busy).ToList();

        Assert.Contains(14 * 60 + 45, slots);
        Assert.DoesNotContain(14 * 60, slots);
        Assert.DoesNotContain(14 * 60 + 30, slots);
        Assert.True(slots.SequenceEqual(slots.OrderBy(m => m)), "offered in time order");
        Assert.Equal(slots.Distinct().Count(), slots.Count);
    }

    [Fact]
    public void A_start_that_would_run_into_a_booking_is_not_offered()
    {
        var busy = new[] { (14 * 60, 14 * 60 + 45) };

        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30, busy).ToList();

        // 13:30 + 45 = 14:15, which is inside the booking.
        Assert.DoesNotContain(13 * 60 + 30, slots);
        Assert.Contains(13 * 60, slots);
    }

    [Fact]
    public void A_gap_too_short_for_the_service_is_not_offered()
    {
        // Two bookings leaving 14:45–15:00: fifteen minutes, and the service needs forty-five.
        var busy = new[] { (14 * 60, 14 * 60 + 45), (15 * 60, 16 * 60) };

        var slots = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30, busy).ToList();

        Assert.DoesNotContain(14 * 60 + 45, slots);
        Assert.Contains(16 * 60, slots);
    }

    [Fact]
    public void With_nothing_booked_the_plain_grid_is_unchanged()
    {
        var withoutBusy = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30).ToList();
        var withEmptyBusy = AvailabilityService.EnumerateSlotStartMinutes(
            new TimeOnly(9, 0), new TimeOnly(18, 0), 45, 30, Array.Empty<(int, int)>()).ToList();

        Assert.Equal(withoutBusy, withEmptyBusy);
    }
}
