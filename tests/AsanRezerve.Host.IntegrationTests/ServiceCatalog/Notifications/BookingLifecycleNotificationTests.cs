using System.Net;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
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
/// Who gets told what, as a booking moves through its life.
/// </summary>
/// <remarks>
/// <para>The rule these exist to hold is that a notification goes to the party who did NOT act. A customer
/// who cancels already knows they cancelled; it is the salon whose day now has a hole in it. Getting this
/// backwards produces notifications that are technically delivered and completely useless, which is the
/// failure mode the old handlers had — they could not tell who cancelled at all.</para>
///
/// <para>Assertions are on the outbox rows rather than the inbox, because what is under test is the
/// addressing decision, and reading it at the point it is made says so more precisely than reading it
/// after delivery.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingLifecycleNotificationTests : ServiceCatalogIntegrationTestBase
{
    public BookingLifecycleNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task When_the_customer_cancels_the_salon_is_told()
    {
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "برنامه‌ام عوض شد", CancelledBy = b.CustomerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingCancelledByCustomer && r.RecipientId == b.OwnerId,
            "the salon has a hole in its day and is the one who can act on it");
    }

    [Fact]
    public async Task When_the_customer_cancels_they_get_their_own_receipt_not_the_salons_notice()
    {
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "برنامه‌ام عوض شد", CancelledBy = b.CustomerId });

        var raised = await RaisedAsync(b.BookingId);

        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingCancelledAck && r.RecipientId == b.CustomerId);
        raised.Should().NotContain(
            r => r.Code == NotificationEventCode.BookingCancelledByProvider,
            "the salon did not cancel this");
    }

    [Fact]
    public async Task When_the_salon_cancels_the_customer_is_told()
    {
        var b = await ArrangeAsync();

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            // No "I am the provider" flag: the actor is inferred from the authenticated caller, so a
            // client cannot claim to be the salon.
            new CancelBookingRequest { Reason = "سالن تعطیل است", CancelledBy = b.OwnerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.BookingCancelledByProvider && r.RecipientId == b.CustomerId,
            "the customer is the one left without an appointment");
    }

    [Fact]
    public async Task Completing_asks_the_customer_for_a_review()
    {
        var b = await ArrangeAsync(imminent: true);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<CompleteBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(
            r => r.Code == NotificationEventCode.ReviewRequest && r.RecipientId == b.CustomerId);
    }

    [Fact]
    public async Task The_review_request_is_scheduled_not_sent_immediately()
    {
        // Asking the moment somebody walks out of the salon is worse than not asking.
        var b = await ArrangeAsync(imminent: true);

        AuthenticateAsProviderOwner(b.Provider);
        await PostAsJsonAsync<CompleteBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });

        var reviewRequests = (await RaisedAsync(b.BookingId))
            .Where(r => r.Code == NotificationEventCode.ReviewRequest)
            .ToList();

        reviewRequests.Should().NotBeEmpty();
        reviewRequests.Should().OnlyContain(r => r.ScheduledFor != null && r.ScheduledFor > DateTime.UtcNow);
    }

    [Fact]
    public async Task A_no_show_tells_the_customer_and_the_salon()
    {
        var b = await ArrangeAsync(past: true);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await PostAsJsonAsync<MarkNoShowRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/no-show",
            new MarkNoShowRequest { Notes = "مراجعه نشد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(r => r.Code == NotificationEventCode.BookingNoShow && r.RecipientId == b.CustomerId);
        raised.Should().Contain(r => r.Code == NotificationEventCode.CustomerNoShow && r.RecipientId == b.OwnerId);
    }

    [Fact]
    public async Task Every_raised_notification_is_addressed_to_a_user_never_to_a_provider_id()
    {
        // A provider id here would address nobody: the inbox, preferences and device registry are all keyed
        // by user. This is a bug that has already happened once, in the reminder scheduler.
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/cancel",
            new CancelBookingRequest { Reason = "برنامه‌ام عوض شد", CancelledBy = b.CustomerId });

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().NotBeEmpty();
        raised.Should().OnlyContain(r => r.RecipientId != b.ProviderId);
    }

    // ── arrange ──

    private sealed record Arranged(
        Guid BookingId,
        Guid CustomerId,
        Guid OwnerId,
        Guid ProviderId,
        Domain.Aggregates.Provider Provider);

    private async Task<Arranged> ArrangeAsync(bool imminent = false, bool past = false)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var customerId = Guid.NewGuid();
        var staffId = await GetBookableMemberIdAsync(provider);

        var startTime = past
            ? DateTime.UtcNow.AddHours(-3)
            : imminent ? DateTime.UtcNow.AddMinutes(5) : DateTime.UtcNow.AddDays(3);

        // The salon path, because it is confirmed on creation — Confirm() needs two hours' notice, which
        // the imminent and past cases cannot satisfy.
        var booking = Booking.CreateConfirmedByProvider(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            staffId,
            startTime,
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "lifecycle");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return new Arranged(booking.Id.Value, customerId, provider.OwnerId.Value, provider.Id.Value, provider);
    }

    private sealed record Raised(NotificationEventCode Code, Guid RecipientId, DateTime? ScheduledFor);

    private async Task<List<Raised>> RaisedAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.SubjectId == bookingId)
            .Select(e => new Raised(e.EventCode, e.RecipientId, e.ScheduledFor))
            .ToListAsync();
    }
}
