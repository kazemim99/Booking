using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Who may cancel a booking. Enforced by <c>AuthorizationBehavior</c> reading
/// <c>BookingOwnershipResolver</c> — the booking's customer or the owning provider's
/// owner-user — rather than by <c>CancelBookingCommandHandler</c> itself, which loads the
/// booking by id with no ownership check at all. That split makes it easy for a future
/// change to the resolver, or to how <c>CancelBookingCommand</c> is marked, to silently stop
/// enforcing ownership without any test noticing: <see cref="ServiceCatalogIntegrationTestBase"/>'s
/// own <c>CancelBooking_AsCustomer_ShouldReturn200Ok</c> only ever authenticates as the true
/// owner, so it cannot catch a hole opening up for anyone else.
///
/// <para>Ported from the Reqnroll <c>Bookings/CancelBooking.feature</c> scenarios "Customer
/// cannot cancel another customer's booking", "Provider can cancel customer booking" and
/// "Cannot cancel booking without authentication" when Reqnroll was retired — these three had
/// no xUnit equivalent; the fourth scenario in that file duplicated
/// <c>CancelBooking_AsCustomer_ShouldReturn200Ok</c>.</para>
/// </summary>
[Collection(ServiceCatalogTestCollection.Name)]
public class BookingCancellationOwnershipTests : ServiceCatalogIntegrationTestBase
{
    public BookingCancellationOwnershipTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private async Task<(Domain.Aggregates.Provider provider, Guid customerId, Booking booking)> ArrangeBookingAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();
        var staffId = await GetBookableMemberIdAsync(provider);

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            staffId,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? Domain.ValueObjects.BookingPolicy.Default,
            "Test booking");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return (provider, customerId, booking);
    }

    private async Task<Domain.Aggregates.Service> GetFirstServiceForProviderAsync(Guid providerId)
    {
        var services = await DbContext.Services
            .Where(s => s.ProviderId == ProviderId.From(providerId))
            .ToListAsync();
        return services.First();
    }

    [Fact]
    public async Task A_Different_Customer_Cannot_Cancel_Someone_Elses_Booking()
    {
        var (_, _, booking) = await ArrangeBookingAsync();

        var strangerId = Guid.NewGuid();
        AuthenticateAsUser(strangerId, "stranger@test.com");

        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "Not my booking", CancelledBy = strangerId });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "the booking's ownership resolver only recognises its own customer and the owning provider");

        DbContext.ChangeTracker.Clear();
        var untouched = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        untouched.Status.Should().Be(BookingStatus.Requested, "a denied cancel must not change the booking");
    }

    [Fact]
    public async Task The_Owning_Providers_Owner_Can_Cancel_A_Customers_Booking()
    {
        var (provider, _, booking) = await ArrangeBookingAsync();

        AuthenticateAsProviderOwner(provider);

        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "Provider schedule conflict", CancelledBy = provider.OwnerId.Value });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "BookingOwnershipResolver resolves the provider's OwnerId as a second valid owner");

        DbContext.ChangeTracker.Clear();
        var cancelled = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        cancelled.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task An_Unauthenticated_Caller_Cannot_Cancel_A_Booking()
    {
        var (_, _, booking) = await ArrangeBookingAsync();

        ClearAuthenticationHeader();

        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "Testing" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
