using AsanRezerve.Core.Domain.Infrastructure.Middleware;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// <c>RescheduleBookingCommandHandler</c> resolves the booking's bookable resource the same way
/// creation does — a membership, or the organization itself (see <c>BookableResourceResolver</c>,
/// which explicitly documents that a legacy "individual sub-provider" third case is gone: any id
/// that is neither the organization nor a real membership 404s). <c>RescheduleBooking_WithValidNewTime</c>
/// in <see cref="BookingsControllerTests"/> only exercises the membership path via
/// <c>GetBookableMemberIdAsync</c>. These tests cover the two resolver outcomes it does not: a
/// booking held directly against the organization, and one whose resource no longer resolves at
/// all — plus the all-or-nothing guarantee when the new time is already taken.
///
/// <para>Ported from Reqnroll's <c>Bookings/RescheduleBooking.feature</c> when Reqnroll was
/// retired. Two of that file's five scenarios are not here: the happy-path membership case
/// duplicates <c>RescheduleBooking_WithValidNewTime</c>, and "legacy individual sub-provider" is,
/// per the resolver's own comment above, mechanically identical to the membership case under the
/// current domain model — both scenarios built their fixture as an
/// <c>OrganizationMembership.CreateUnclaimed</c>, there being no other kind of resource left to
/// build.</para>
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class RescheduleResourceResolutionTests : ServiceCatalogIntegrationTestBase
{
    public RescheduleResourceResolutionTests(AsanRezerveHostFactory factory)
        : base(factory) { }

    private async Task<Domain.Aggregates.Service> GetFirstServiceForProviderAsync(Guid providerId)
    {
        var services = await DbContext.Services
            .Where(s => s.ProviderId == ProviderId.From(providerId))
            .ToListAsync();
        return services.First();
    }

    private async Task<(Domain.Aggregates.Provider provider, Domain.Aggregates.Service service, Guid customerId, Booking booking)>
        ArrangeBookingForResourceAsync(Guid resourceId)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            resourceId,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "Test booking");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return (provider, service, customerId, booking);
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    [Fact]
    public async Task Rescheduling_A_Booking_Held_Directly_Against_The_Organization_Succeeds()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        // resourceId == provider.Id: a solo/direct booking, resolved by
        // BookableResourceResolver's first branch rather than the membership lookup.
        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "Test booking");
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        var newStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14);

        var response = await PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule",
            new RescheduleBookingRequest { NewStartTime = newStartTime, Reason = "Schedule conflict" });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            response.Error is { } e ? $"{e.Code}: {e.Message}" : "no error payload");

        DbContext.ChangeTracker.Clear();
        var old = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        old.Status.Should().Be(BookingStatus.Rescheduled);

        var successor = await DbContext.Bookings.FirstOrDefaultAsync(b => b.PreviousBookingId == booking.Id);
        successor.Should().NotBeNull();
        successor!.StaffId.Should().Be(provider.Id.Value,
            "the successor keeps the same resource — the organization itself");
    }

    [Fact]
    public async Task A_Rejected_Reschedule_Leaves_The_Original_Booking_Untouched()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var resourceId = await GetBookableMemberIdAsync(provider);
        var customerId = Guid.NewGuid();

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            resourceId,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "Test booking");
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        var newStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14);

        // Someone else already occupies the slot this reschedule is aimed at, for the SAME
        // resource — the handler must refuse the move rather than free the old slot first.
        var blocker = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            resourceId,
            newStartTime,
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "Occupies the target slot");
        DbContext.Bookings.Add(blocker);
        await DbContext.SaveChangesAsync();

        AuthenticateAsUser(customerId, "customer@test.com");

        var response = await PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule",
            new RescheduleBookingRequest { NewStartTime = newStartTime, Reason = "Schedule conflict" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        DbContext.ChangeTracker.Clear();
        var untouched = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        untouched.Status.Should().Be(BookingStatus.Requested,
            "a rejected reschedule must not release the original slot before the new one is confirmed available");
        // BeCloseTo, not Be: Postgres timestamptz round-trips to microsecond precision, one digit
        // short of a .NET DateTime tick.
        untouched.TimeSlot.StartTime.Should().BeCloseTo(booking.TimeSlot.StartTime, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Rescheduling_A_Booking_Whose_Resource_No_Longer_Resolves_Fails_With_404()
    {
        // Neither the organization's own id nor a real membership — BookableResourceResolver
        // has nothing else a staff reference can mean.
        var (_, _, customerId, booking) = await ArrangeBookingForResourceAsync(Guid.NewGuid());

        AuthenticateAsUser(customerId, "customer@test.com");
        var newStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14);

        var response = await PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule",
            new RescheduleBookingRequest { NewStartTime = newStartTime, Reason = "Schedule conflict" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        DbContext.ChangeTracker.Clear();
        var untouched = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        untouched.Status.Should().Be(BookingStatus.Requested, "a failed reschedule must not alter the booking");
    }
}
