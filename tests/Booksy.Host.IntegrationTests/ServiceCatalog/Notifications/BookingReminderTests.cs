using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Reminders follow the appointment: scheduled when it becomes real, moved when it moves, withdrawn when it
/// stops being a future event.
/// </summary>
/// <remarks>
/// The failure these guard against is the one a customer actually notices — being told to turn up for an
/// appointment that was cancelled, or at an hour it was moved away from.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class BookingReminderTests : ServiceCatalogIntegrationTestBase
{
    public BookingReminderTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Confirming_a_future_booking_schedules_its_reminders()
    {
        var booking = await GivenConfirmedBookingAsync(startsIn: TimeSpan.FromDays(3));

        var scheduled = await RemindersForAsync(booking.Id.Value);

        scheduled.Should().Contain(NotificationEventCode.BookingReminder24h);
        scheduled.Should().Contain(NotificationEventCode.BookingReminder2h);
        scheduled.Should().Contain(NotificationEventCode.NextAppointmentReminder);
    }

    [Fact]
    public async Task A_reminder_whose_moment_has_already_passed_is_not_scheduled()
    {
        // Booked an hour beforehand: the day-before reminder's moment is gone. It must be skipped, not fired
        // immediately — which is what a naive "schedule everything" would do.
        var booking = await GivenConfirmedBookingAsync(startsIn: TimeSpan.FromHours(1));

        var scheduled = await RemindersForAsync(booking.Id.Value);

        scheduled.Should().NotContain(NotificationEventCode.BookingReminder24h);
        scheduled.Should().NotContain(NotificationEventCode.BookingReminder2h);
    }

    [Fact]
    public async Task Reminders_are_scheduled_for_the_future_not_for_now()
    {
        var startsIn = TimeSpan.FromDays(2);
        var booking = await GivenConfirmedBookingAsync(startsIn);

        var rows = await RowsForAsync(booking.Id.Value);

        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(
            r => r.ScheduledFor != null && r.ScheduledFor > DateTime.UtcNow,
            "a reminder that is due immediately is not a reminder");
    }

    [Fact]
    public async Task Scheduling_twice_does_not_double_the_reminders()
    {
        // A re-confirmed or retried booking must not give the customer two of everything.
        var booking = await GivenConfirmedBookingAsync(startsIn: TimeSpan.FromDays(3));
        await ScheduleAsync(booking);

        var rows = await RowsForAsync(booking.Id.Value);

        rows.Select(r => r.EventCode).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Withdrawing_cancels_every_unsent_reminder()
    {
        var booking = await GivenConfirmedBookingAsync(startsIn: TimeSpan.FromDays(3));

        using (var scope = Factory.Services.CreateScope())
        {
            var reminders = scope.ServiceProvider.GetRequiredService<IBookingReminderScheduler>();
            await reminders.WithdrawAsync(booking.Id.Value);
        }

        var rows = await RowsForAsync(booking.Id.Value);

        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(
            r => r.State == NotificationOutboxState.Cancelled,
            "an appointment that is off must not remind anybody");
    }

    // ── helpers ──

    private async Task<Booking> GivenConfirmedBookingAsync(TimeSpan startsIn)
    {
        var booking = await BookingForAsync(DateTime.UtcNow.Add(startsIn));
        await ScheduleAsync(booking);
        return booking;
    }

    /// <summary>
    /// Builds a booking against a REAL provider. The booking flow itself is not under test, but the
    /// provider has to exist: the salon's reminder is addressed to its owner's user id, and a fabricated
    /// provider id has no owner to address — so inventing one would silently drop that reminder and the
    /// test would be asserting against a fiction.
    /// </summary>
    private async Task<Booking> BookingForAsync(DateTime startTime)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        return Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            staffId: Guid.NewGuid(),
            startTime: startTime,
            duration: Duration.FromMinutes(60),
            totalPrice: Price.Create(100, "IRR"),
            policy: BookingPolicy.Default);
    }

    private async Task ScheduleAsync(Booking booking)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var reminders = scope.ServiceProvider.GetRequiredService<IBookingReminderScheduler>();

        await reminders.ScheduleAsync(booking);
        await context.SaveChangesAsync();
    }

    private async Task<List<NotificationOutboxEntry>> RowsForAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.SubjectType == BookingReminderScheduler.BookingSubject && e.SubjectId == bookingId)
            .ToListAsync();
    }

    private async Task<List<NotificationEventCode>> RemindersForAsync(Guid bookingId) =>
        (await RowsForAsync(bookingId)).Select(r => r.EventCode).ToList();
}
