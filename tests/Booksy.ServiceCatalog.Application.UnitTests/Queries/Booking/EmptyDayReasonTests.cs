using Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.Application.UnitTests.Queries.Booking;

/// <summary>
/// A provider picked Sunday and was told «no qualified staff member is available», though the staff
/// member was qualified: the salon's Sunday was 11:00–12:00 and the service takes 90 minutes
/// (production, 2026-09-19). An empty day must name the reason the provider can act on.
/// </summary>
public class EmptyDayReasonTests
{
    [Fact]
    public void Hours_shorter_than_the_service_are_named_with_the_times()
    {
        var reason = EmptyDayReason.Describe(new TimeOnly(11, 0), new TimeOnly(12, 0), serviceMinutes: 90);

        reason.Should().Contain("11:00").And.Contain("12:00").And.Contain("90");
        reason.Should().NotContain("کارمند");
    }

    [Fact]
    public void Hours_long_enough_fall_back_to_the_staff_reason()
    {
        EmptyDayReason.Describe(new TimeOnly(9, 0), new TimeOnly(18, 0), serviceMinutes: 90)
            .Should().Be(EmptyDayReason.NoQualifiedStaff);
    }

    [Fact]
    public void A_closed_day_says_the_salon_is_closed()
    {
        EmptyDayReason.Describe(open: null, close: null, serviceMinutes: 30)
            .Should().Be(EmptyDayReason.Closed);
    }

    [Fact]
    public void A_service_exactly_as_long_as_the_day_fits()
    {
        EmptyDayReason.Describe(new TimeOnly(11, 0), new TimeOnly(12, 0), serviceMinutes: 60)
            .Should().Be(EmptyDayReason.NoQualifiedStaff);
    }
}
