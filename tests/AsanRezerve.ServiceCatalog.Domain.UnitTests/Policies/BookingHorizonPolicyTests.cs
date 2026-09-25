using AsanRezerve.ServiceCatalog.Domain.Policies;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.Policies;

/// <summary>
/// The public booking window (QA walkthrough 2026-09-22): a customer books at most a week ahead, and the date
/// picker offers only those days. A salon's own book is not bound by it.
/// </summary>
public class BookingHorizonPolicyTests
{
    private static readonly DateTime Today = new(2026, 9, 22);

    [Fact]
    public void The_platform_window_is_a_week()
    {
        Assert.Equal(7, BookingHorizonPolicy.PlatformMaxAdvanceDays);
        Assert.Equal(7, BookingHorizonPolicy.EffectiveMaxAdvanceDays());
    }

    [Fact]
    public void A_shorter_service_or_salon_window_wins()
    {
        Assert.Equal(3, BookingHorizonPolicy.EffectiveMaxAdvanceDays(serviceDays: 3));
        Assert.Equal(2, BookingHorizonPolicy.EffectiveMaxAdvanceDays(providerDays: 2));
        Assert.Equal(2, BookingHorizonPolicy.EffectiveMaxAdvanceDays(serviceDays: 5, providerDays: 2));
    }

    [Fact]
    public void A_longer_stored_window_is_capped()
    {
        // Every salon's stored policy still says 90; the platform rule has to hold anyway.
        Assert.Equal(7, BookingHorizonPolicy.EffectiveMaxAdvanceDays(serviceDays: 90, providerDays: 90));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(7, false)]
    [InlineData(8, true)]
    public void The_seventh_day_is_inside_the_window_and_the_eighth_is_not(int daysAhead, bool beyond)
    {
        Assert.Equal(beyond, BookingHorizonPolicy.IsBeyondWindow(Today.AddDays(daysAhead), Today));
    }

    [Fact]
    public void The_message_names_the_window_that_applied()
    {
        Assert.Contains("7", BookingHorizonPolicy.BeyondWindowMessage());
        Assert.Contains("3", BookingHorizonPolicy.BeyondWindowMessage(serviceDays: 3));
    }
}
