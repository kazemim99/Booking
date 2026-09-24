using System.Net;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Booksy.ServiceCatalog.IntegrationTests.API.Bookings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Who is told when an appointment moves.
/// </summary>
/// <remarks>
/// <para>Rescheduling closes one booking and opens another, so the notification has to follow the NEW
/// booking — a notice filed against the old one would be withdrawn along with its reminders and never
/// arrive.</para>
///
/// <para>Same addressing rule as cancellation: the party who did not move it is the one who needs telling,
/// and the actor is taken from the authenticated caller rather than a request field.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class RescheduleNotificationTests : ServiceCatalogIntegrationTestBase
{
    public RescheduleNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task When_the_customer_moves_it_the_salon_is_told()
    {
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        var response = await RescheduleAsync(b, b.StartTime);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedForProviderAsync(b.OwnerId);
        raised.Should().Contain(NotificationEventCode.BookingRescheduledByCustomer);
    }

    [Fact]
    public async Task When_the_salon_moves_it_the_customer_is_told()
    {
        var b = await ArrangeAsync();

        AuthenticateAsProviderOwner(b.Provider);
        var response = await RescheduleAsync(b, b.StartTime);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raised = await RaisedForCustomerAsync(b.CustomerId);
        raised.Should().Contain(NotificationEventCode.BookingRescheduled);
    }

    [Fact]
    public async Task The_notice_is_filed_against_the_new_booking_not_the_old_one()
    {
        // The old booking's rows are withdrawn when it closes. A notice filed there would be cancelled
        // before it could ever be sent.
        var b = await ArrangeAsync();

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await RescheduleAsync(b, b.StartTime);

        var onOldBooking = await StatesForSubjectAsync(b.BookingId);
        onOldBooking.Should().NotContain(
            NotificationOutboxStateNames.Pending,
            "everything still attached to the closed booking should have been withdrawn");
    }

    [Fact]
    public async Task The_reminders_move_to_the_new_booking()
    {
        // This replaces an assertion of mine that asked whether ANY pending reminder existed anywhere in the
        // table, unscoped to this booking. Measured rather than assumed: with the handler's ScheduleAsync
        // removed, that unscoped assertion DID still fail, so it was not vacuous — the claim that it proved
        // nothing was wrong. What it could not tell apart is a reminder that moved to the successor from one
        // that was left on the closed booking, and those are opposite outcomes. Hence the two assertions
        // below, which name the booking each reminder must be on.
        var b = await ArrangeAsync();
        var newStart = b.StartTime;

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await RescheduleAsync(b, newStart);

        var successorId = await SuccessorOfAsync(b.BookingId);
        successorId.Should().NotBeNull("rescheduling closes one booking and opens another");

        (await PendingRemindersForAsync(successorId!.Value))
            .Should().Contain(NotificationEventCode.BookingReminder24h)
            .And.Contain(NotificationEventCode.BookingReminder2h);

        (await PendingRemindersForAsync(b.BookingId)).Should().BeEmpty(
            "reminders left on the closed booking would announce an appointment nobody is keeping");
    }

    [Fact]
    public async Task The_moved_reminders_are_timed_from_the_new_start()
    {
        // Moving the rows without moving the times would be the subtler failure: the customer still gets a
        // "tomorrow" reminder, just keyed to the appointment they no longer have.
        var b = await ArrangeAsync();
        var newStart = b.StartTime;

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await RescheduleAsync(b, newStart);

        var successorId = await SuccessorOfAsync(b.BookingId);
        var due = await ReminderDueTimesAsync(successorId!.Value);

        due[NotificationEventCode.BookingReminder24h]
            .Should().BeCloseTo(Core.Domain.ValueObjects.SalonTime.ToUtc(newStart.AddHours(-24)), TimeSpan.FromMinutes(1));
        due[NotificationEventCode.BookingReminder2h]
            .Should().BeCloseTo(Core.Domain.ValueObjects.SalonTime.ToUtc(newStart.AddHours(-2)), TimeSpan.FromMinutes(1));
    }

    // ── helpers ──

    private Task<Core.Domain.Infrastructure.Middleware.ApiResponse<BookingMessagePayload>> RescheduleAsync(
        Arranged b,
        DateTime newStart) =>
        PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{b.BookingId}/reschedule",
            new RescheduleBookingRequest { NewStartTime = newStart, Reason = "جابه‌جایی" });

    private sealed record Arranged(
        Guid BookingId,
        Guid CustomerId,
        Guid OwnerId,
        DateTime StartTime,
        Domain.Aggregates.Provider Provider);

    /// <summary>
    /// A direct booking against the salon itself, mirroring the arrange the existing reschedule tests use.
    /// </summary>
    /// <remarks>
    /// The staff id is the provider id (a solo/direct booking), which is the branch the resource resolver
    /// handles without a membership lookup. A member id needs availability rows that this fixture does not
    /// create, and the reschedule is then refused before any notification is reached.
    /// </remarks>
    private async Task<Arranged> ArrangeAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var customerId = Guid.NewGuid();

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "reschedule");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return new Arranged(
            booking.Id.Value,
            customerId,
            provider.OwnerId.Value,
            NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14),
            provider);
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private async Task<List<NotificationEventCode>> RaisedForCustomerAsync(Guid customerId) =>
        await CodesAsync(e => e.RecipientId == customerId);

    private async Task<List<NotificationEventCode>> RaisedForProviderAsync(Guid ownerId) =>
        await CodesAsync(e => e.RecipientId == ownerId);

    private async Task<List<NotificationEventCode>> CodesAsync(
        System.Linq.Expressions.Expression<Func<NotificationOutboxEntry, bool>> where)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox.AsNoTracking().Where(where)
            .Select(e => e.EventCode).ToListAsync();
    }

    private async Task<List<string>> StatesForSubjectAsync(Guid subjectId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == subjectId)
            .Select(e => e.State).ToListAsync();
    }

    /// <summary>The booking a reschedule opened in place of this one.</summary>
    private async Task<Guid?> SuccessorOfAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var previous = Domain.ValueObjects.BookingId.From(bookingId);

        var successor = await context.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.PreviousBookingId == previous);

        return successor?.Id.Value;
    }

    /// <summary>
    /// Scoped to one booking, deliberately. Asking the whole table whether a reminder exists proves nothing
    /// in a collection where every other test leaves reminders lying around.
    /// </summary>
    private async Task<List<NotificationEventCode>> PendingRemindersForAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId
                        && e.State == NotificationOutboxStateNames.Pending
                        && (e.EventCode == NotificationEventCode.BookingReminder24h
                            || e.EventCode == NotificationEventCode.BookingReminder2h))
            .Select(e => e.EventCode).ToListAsync();
    }

    private async Task<Dictionary<NotificationEventCode, DateTime>> ReminderDueTimesAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var rows = await context.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId && e.ScheduledFor != null)
            .Select(e => new { e.EventCode, e.ScheduledFor })
            .ToListAsync();

        return rows
            .GroupBy(r => r.EventCode)
            .ToDictionary(g => g.Key, g => g.First().ScheduledFor!.Value);
    }

    /// <summary>Local alias so the test reads without pulling the domain policy namespace in twice.</summary>
    private static class NotificationOutboxStateNames
    {
        public const string Pending = "Pending";
    }
}
