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
    public async Task The_moved_booking_gets_reminders_for_its_new_time()
    {
        var b = await ArrangeAsync();
        var newStart = b.StartTime;

        AuthenticateAsUser(b.CustomerId, "customer@test.com");
        await RescheduleAsync(b, newStart);

        var reminders = await PendingRemindersAsync();
        reminders.Should().NotBeEmpty("the appointment still exists, just later");
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

    private async Task<List<NotificationEventCode>> PendingRemindersAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox.AsNoTracking()
            .Where(e => e.State == NotificationOutboxStateNames.Pending
                        && (e.EventCode == NotificationEventCode.BookingReminder24h
                            || e.EventCode == NotificationEventCode.BookingReminder2h))
            .Select(e => e.EventCode).ToListAsync();
    }

    /// <summary>Local alias so the test reads without pulling the domain policy namespace in twice.</summary>
    private static class NotificationOutboxStateNames
    {
        public const string Pending = "Pending";
    }
}
