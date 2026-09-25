using System.Net;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Telling a customer their request was declined, rather than that their appointment was cancelled.
/// </summary>
/// <remarks>
/// <para><b>There is no reject flow to build — there already is one.</b> The task list assumed a rejection
/// needed inventing, and the investigation says otherwise: a <c>Requested</c> booking is cancellable
/// (<c>CanBeCancelled</c>), the cancel endpoint infers the actor from the authenticated caller, so a salon
/// declining a request is a transition the domain already performs. What was missing is only that the
/// customer was told the wrong thing about it.</para>
///
/// <para><b>The distinction is the starting state, not the destination.</b> Both land on <c>Cancelled</c>.
/// A request the salon never accepted is a REJECTION — the customer never had an appointment. A confirmed
/// booking the salon calls off is a CANCELLATION — they did, and it has been taken away. Same row, two
/// different messages, and reading them the wrong way round is how a customer ends up thinking they lost
/// an appointment they never had.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingRejectionNotificationTests : ServiceCatalogIntegrationTestBase
{
    public BookingRejectionNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_salon_declining_a_request_tells_the_customer_it_was_rejected()
    {
        var b = await ArrangeAsync(confirmed: false);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "آن ساعت پر است", CancelledBy = b.OwnerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingRejected && r.RecipientId == b.CustomerId);
        raised.Should().NotContain(
            r => r.Code == NotificationEventCode.BookingCancelledByProvider,
            "they never had an appointment to lose");
    }

    [Fact]
    public async Task A_salon_calling_off_a_confirmed_booking_is_still_a_cancellation()
    {
        var b = await ArrangeAsync(confirmed: true);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "استاد مریض شد", CancelledBy = b.OwnerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingCancelledByProvider && r.RecipientId == b.CustomerId);
        raised.Should().NotContain(r => r.Code == NotificationEventCode.BookingRejected);
    }

    [Fact]
    public async Task A_customer_withdrawing_their_own_request_is_not_a_rejection()
    {
        // The actor still decides. A customer changing their mind before the salon answered gets their own
        // receipt, and the salon is told — nobody rejected anything.
        var b = await ArrangeAsync(confirmed: false);

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "پشیمان شدم", CancelledBy = b.CustomerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().NotContain(r => r.Code == NotificationEventCode.BookingRejected);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingCancelledAck && r.RecipientId == b.CustomerId);
    }

    [Fact]
    public async Task A_rejection_is_addressed_to_the_customer_and_nobody_else()
    {
        var b = await ArrangeAsync(confirmed: false);

        AuthenticateAsProviderOwner(b.Provider);
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "آن ساعت پر است", CancelledBy = b.OwnerId });

        var rejections = (await RaisedAsync(b.BookingId))
            .Where(r => r.Code == NotificationEventCode.BookingRejected)
            .ToList();

        rejections.Should().ContainSingle();
        rejections[0].RecipientId.Should().Be(b.CustomerId, "the salon decided it; it does not need telling");
    }

    // ── arrange ──

    private sealed record Arranged(Guid BookingId, Guid CustomerId, Guid OwnerId, Domain.Aggregates.Provider Provider);

    private async Task<Arranged> ArrangeAsync(bool confirmed)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(3);

        var booking = confirmed
            ? Booking.CreateConfirmedByProvider(
                UserId.From(customerId), provider.Id, service.Id, provider.Id.Value, start,
                service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "reject")
            : Booking.CreateBookingRequest(
                UserId.From(customerId), provider.Id, service.Id, provider.Id.Value, start,
                service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "reject");

        await CreateEntityAsync(booking);

        return new Arranged(booking.Id.Value, customerId, provider.OwnerId.Value, provider);
    }

    private sealed record Raised(NotificationEventCode Code, Guid RecipientId);

    private async Task<List<Raised>> RaisedAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId)
            .Select(e => new Raised(e.EventCode, e.RecipientId))
            .ToListAsync();
    }
}
