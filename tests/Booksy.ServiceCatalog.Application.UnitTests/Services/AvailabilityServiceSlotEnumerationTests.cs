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
}
