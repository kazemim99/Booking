using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Asking a customer for a review — twice at most, and never after they have left one.
/// </summary>
/// <remarks>
/// <para><b>Withdrawal happens at SUBMISSION, not at publication.</b> Reviews are admin-moderated
/// (booking-d2, 2026-09-21), so a review can exist while still invisible. Keying the withdrawal off anything
/// publication-shaped would nag the one group who must never be nagged: people who did leave a review and
/// are waiting on a moderator. These tests pin the hook to the moment the review is accepted, which is why
/// they go through <c>POST /reviews/bookings/{id}</c> and assert immediately, with no approval step.</para>
///
/// <para><b>Consequence, accepted knowingly:</b> a REJECTED review means the customer is never asked again.
/// The reminder was withdrawn at submission and nothing re-raises it. The alternative is worse — the
/// notification system cannot see WHY a review was rejected, so re-asking would either invite the same
/// refused content back or read as "we ignored you, try again".</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ReviewRequestNotificationTests : ServiceCatalogIntegrationTestBase
{
    /// <summary>The two ways this product asks for a review. Nothing else counts as asking.</summary>
    private static readonly NotificationEventCode[] TheAsk =
    {
        NotificationEventCode.ReviewRequest,
        NotificationEventCode.ReviewReminder,
    };

    public ReviewRequestNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Completing_a_booking_asks_once_soon_and_once_later()
    {
        var arranged = await CompleteABookingAsync();

        var asks = (await RaisedForAsync(arranged.BookingId))
            .Where(r => TheAsk.Contains(r.Code))
            .ToList();

        asks.Should().HaveCount(2);

        var soon = asks.Single(a => a.Code == NotificationEventCode.ReviewRequest);
        var later = asks.Single(a => a.Code == NotificationEventCode.ReviewReminder);

        // Asking as somebody walks out of the salon is worse than not asking.
        soon.ScheduledFor.Should().NotBeNull();
        soon.ScheduledFor!.Value.Should().BeCloseTo(arranged.At.AddHours(2), TimeSpan.FromMinutes(5));

        later.ScheduledFor.Should().NotBeNull();
        later.ScheduledFor!.Value.Should().BeCloseTo(arranged.At.AddDays(3), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Leaving_a_review_stops_the_later_one()
    {
        var arranged = await CompleteABookingAsync();

        await LeaveAReviewAsync(arranged);

        var later = (await RaisedForAsync(arranged.BookingId))
            .Single(r => r.Code == NotificationEventCode.ReviewReminder);

        later.State.Should().Be(
            NotificationOutboxState.Cancelled,
            "nagging somebody who already reviewed is the failure this whole hook exists to prevent");
    }

    [Fact]
    public async Task Leaving_a_review_stops_the_first_one_too_if_it_has_not_gone_yet()
    {
        // Somebody who reviews within two hours of walking out has answered the question before it was
        // asked. Asking anyway would be absurd.
        var arranged = await CompleteABookingAsync();

        await LeaveAReviewAsync(arranged);

        var soon = (await RaisedForAsync(arranged.BookingId))
            .Single(r => r.Code == NotificationEventCode.ReviewRequest);

        soon.State.Should().Be(NotificationOutboxState.Cancelled);
    }

    [Fact]
    public async Task Leaving_a_review_does_not_silence_anything_else_about_the_booking()
    {
        // The withdrawal used by cancellation is deliberately blunt — it cancels EVERY unsent notification
        // filed under the booking — and that is right when the appointment is off, because none of them
        // should go. It is wrong here: the booking still happened, and a review says nothing about, say, a
        // pending refund notice. So this withdrawal names the codes it is allowed to touch.
        var arranged = await CompleteABookingAsync();
        await RaiseUnrelatedNotificationAsync(arranged, NotificationEventCode.RefundProcessed);

        await LeaveAReviewAsync(arranged);

        var unrelated = (await RaisedForAsync(arranged.BookingId))
            .Single(r => r.Code == NotificationEventCode.RefundProcessed);

        unrelated.State.Should().Be(
            NotificationOutboxState.Pending,
            "a review is not a reason to swallow an unrelated notice about the same booking");
    }

    [Fact]
    public async Task A_customer_is_never_asked_more_than_twice()
    {
        var arranged = await CompleteABookingAsync();

        // A retried command, a redelivered event — the handler may legitimately run again.
        await CompleteAgainAsync(arranged);

        (await RaisedForAsync(arranged.BookingId))
            .Count(r => TheAsk.Contains(r.Code))
            .Should().Be(2);
    }

    // ── arrange ──

    private sealed record Arranged(Guid BookingId, Guid CustomerId, Domain.Aggregates.Provider Provider, DateTime At);

    /// <summary>
    /// A booking taken through the real complete endpoint, which is where the two asks are raised.
    /// </summary>
    /// <remarks>
    /// <c>Complete()</c> is only legal within fifteen minutes of the start, and <c>Confirm()</c> demands two
    /// hours' notice, so no request-then-confirm path can produce a completable booking. The salon path
    /// (<c>CreateConfirmedByProvider</c>) is confirmed on creation and is the only way in.
    /// </remarks>
    private async Task<Arranged> CompleteABookingAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();

        var booking = Booking.CreateConfirmedByProvider(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddMinutes(5),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "review request");

        await CreateEntityAsync(booking);

        var at = DateTime.UtcNow;

        AuthenticateAsProviderOwner(provider);
        var response = await PostAsJsonAsync<CompleteBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return new Arranged(booking.Id.Value, customerId, provider, at);
    }

    /// <summary>Completing a second time is refused by the domain; the outbox must still hold two asks.</summary>
    private async Task CompleteAgainAsync(Arranged arranged)
    {
        AuthenticateAsProviderOwner(arranged.Provider);
        await PostAsJsonAsync<CompleteBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{arranged.BookingId}/complete",
            new CompleteBookingRequest { CompletionNotes = "دوباره" });
    }

    private async Task LeaveAReviewAsync(Arranged arranged)
    {
        AuthenticateAsUser(arranged.CustomerId, "customer@test.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/reviews/bookings/{arranged.BookingId}",
            new { rating = 5.0m, comment = "خدمات عالی بود، ممنون از تیم شما." });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, "review failed: " + body);
    }

    private async Task RaiseUnrelatedNotificationAsync(Arranged arranged, NotificationEventCode code)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

        await raiser.RaiseAsync(
            code,
            arranged.CustomerId,
            dedupKey: arranged.BookingId,
            parameters: new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
            subjectType: BookingReminderScheduler.BookingSubject,
            subjectId: arranged.BookingId,
            scheduledFor: DateTime.UtcNow.AddDays(1));

        await context.SaveChangesAsync();
    }

    private sealed record Raised(NotificationEventCode Code, string State, DateTime? ScheduledFor);

    private async Task<List<Raised>> RaisedForAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId)
            .Select(e => new Raised(e.EventCode, e.State, e.ScheduledFor))
            .ToListAsync();
    }
}
