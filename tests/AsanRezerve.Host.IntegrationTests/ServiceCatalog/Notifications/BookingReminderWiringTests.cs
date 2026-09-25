using System.Net;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// That the booking commands actually withdraw a booking's reminders.
/// </summary>
/// <remarks>
/// <para><c>BookingReminderTests</c> calls <see cref="IBookingReminderScheduler"/> directly, which proves the
/// scheduler and nothing about the five handlers that were changed to call it. This goes through the real
/// command, so the call site itself is what is under test — that is where a missed wiring would actually
/// live, and it is invisible to a test that skips the handler.</para>
///
/// <para>The failure being prevented is concrete: a customer cancels, and two hours before the appointment
/// they were not going to attend, their phone tells them to come.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingReminderWiringTests : ServiceCatalogIntegrationTestBase
{
    public BookingReminderWiringTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cancelling_a_booking_through_the_api_withdraws_its_reminders()
    {
        var (customerId, booking, _) = await ArrangeBookingWithRemindersAsync();

        (await RemindersFor(booking)).Should().NotBeEmpty("the booking must start with reminders to withdraw");

        AuthenticateAsUser(customerId, "customer@test.com");
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "تغییر برنامه", CancelledBy = customerId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var states = await ReminderStatesAsync(booking.Id.Value);
        states.Should().NotBeEmpty();
        states.Should().OnlyContain(
            s => s == NotificationOutboxState.Cancelled,
            "an appointment that is off must not remind anybody");
    }

    [Fact]
    public async Task A_cancelled_bookings_reminders_are_not_merely_left_pending()
    {
        // The specific regression: the handler runs, the booking is cancelled, and nobody touched the
        // outbox — so the reminders sit there Pending and fire on schedule.
        var (customerId, booking, _) = await ArrangeBookingWithRemindersAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "تغییر برنامه", CancelledBy = customerId });

        var states = await ReminderStatesAsync(booking.Id.Value);
        states.Should().NotContain(NotificationOutboxState.Pending);
    }

    [Fact]
    public async Task Another_bookings_reminders_are_untouched_by_a_cancellation()
    {
        // Withdrawal is scoped by subject id. If it were not, cancelling one appointment would silence
        // every other customer's reminders too.
        var (customerId, cancelled, _) = await ArrangeBookingWithRemindersAsync();
        var (_, untouched, _) = await ArrangeBookingWithRemindersAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{cancelled.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "تغییر برنامه", CancelledBy = customerId });

        (await ReminderStatesAsync(untouched.Id.Value))
            .Should().OnlyContain(s => s == NotificationOutboxState.Pending);
    }

    [Fact]
    public async Task Completing_a_booking_withdraws_its_remaining_reminders()
    {
        // The appointment happened. A "your appointment is in 2 hours" afterwards is noise at best.
        // Complete is legal from fifteen minutes before the start.
        var (booking, provider) = await ArrangeImminentBookingWithAPendingReminderAsync(TimeSpan.FromMinutes(5));

        AuthenticateAsProviderOwner(provider);
        var response = await PostAsJsonAsync<CompleteBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/complete",
            new CompleteBookingRequest { CompletionNotes = "انجام شد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReminderStatesAsync(booking.Id.Value))
            .Should().OnlyContain(s => s == NotificationOutboxState.Cancelled);
    }

    [Fact]
    public async Task Marking_a_no_show_withdraws_its_remaining_reminders()
    {
        // No-show is only legal once the appointment is over, so this one is in the past.
        var (booking, provider) = await ArrangeImminentBookingWithAPendingReminderAsync(TimeSpan.FromHours(-3));

        AuthenticateAsProviderOwner(provider);
        var response = await PostAsJsonAsync<MarkNoShowRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/no-show",
            new MarkNoShowRequest { Notes = "مراجعه نشد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReminderStatesAsync(booking.Id.Value))
            .Should().OnlyContain(s => s == NotificationOutboxState.Cancelled);
    }

    /// <summary>
    /// A booking that is about to start, carrying one still-unsent notification about it.
    /// </summary>
    /// <remarks>
    /// Complete and no-show are only legal within fifteen minutes of the start time, and by then every
    /// reminder offset has elapsed — so the scheduler would correctly create nothing. The row is therefore
    /// seeded directly: what is under test is the handler's withdrawal call, not how the row got there.
    /// </remarks>
    private async Task<(Booking Booking, Domain.Aggregates.Provider Provider)>
        ArrangeImminentBookingWithAPendingReminderAsync(TimeSpan startsIn)
    {
        var (_, booking, provider) = await ArrangeBookingWithRemindersAsync(
            confirmed: true, startsIn: startsIn, scheduleReminders: false);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

        await raiser.RaiseAsync(
            Domain.Enums.NotificationEventCode.BookingReminder2h,
            booking.CustomerId.Value,
            dedupKey: booking.Id.Value,
            parameters: new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
            subjectType: BookingReminderScheduler.BookingSubject,
            subjectId: booking.Id.Value,
            scheduledFor: DateTime.UtcNow.AddHours(1));

        await context.SaveChangesAsync();

        return (booking, provider);
    }

    // ── arrange ──

    private async Task<(Guid CustomerId, Booking Booking, Domain.Aggregates.Provider Provider)> ArrangeBookingWithRemindersAsync(
        bool confirmed = false,
        TimeSpan? startsIn = null,
        bool scheduleReminders = true)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var customerId = Guid.NewGuid();
        var staffId = await GetBookableMemberIdAsync(provider);

        var startTime = DateTime.UtcNow.Add(startsIn ?? TimeSpan.FromDays(3));

        // A salon-entered booking is confirmed on creation, which is the only way to get a Confirmed
        // booking that starts soon: Confirm() requires two hours' notice, and Complete() requires the start
        // to be within fifteen minutes. Nothing can satisfy both through the request-then-confirm path.
        var booking = confirmed
            ? Booking.CreateConfirmedByProvider(
                Core.Domain.ValueObjects.UserId.From(customerId),
                provider.Id,
                service.Id,
                staffId,
                startTime,
                service.Duration,
                service.BasePrice,
                service.BookingPolicy ?? BookingPolicy.Default,
                "reminder wiring")
            : Booking.CreateBookingRequest(
                Core.Domain.ValueObjects.UserId.From(customerId),
                provider.Id,
                service.Id,
                staffId,
                startTime,
                service.Duration,
                service.BasePrice,
                service.BookingPolicy ?? BookingPolicy.Default,
                "reminder wiring");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        if (scheduleReminders)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var reminders = scope.ServiceProvider.GetRequiredService<IBookingReminderScheduler>();
            await reminders.ScheduleAsync(booking);
            await context.SaveChangesAsync();
        }

        return (customerId, booking, provider);
    }

    /// <summary>
    /// The states of the REMINDER rows for a booking.
    /// </summary>
    /// <remarks>
    /// Filtered by code on purpose. The handlers raise a fresh notification about the cancellation or
    /// completion itself, filed under the same booking subject, and that row is legitimately Pending — so
    /// "every row for this booking is Cancelled" would be asserting something false.
    /// </remarks>
    private static readonly NotificationEventCode[] ReminderCodes =
    {
        NotificationEventCode.BookingReminder24h,
        NotificationEventCode.BookingReminder2h,
        NotificationEventCode.NextAppointmentReminder,
    };

    private async Task<List<string>> ReminderStatesAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.SubjectType == BookingReminderScheduler.BookingSubject
                        && e.SubjectId == bookingId
                        && ReminderCodes.Contains(e.EventCode))
            .Select(e => e.State)
            .ToListAsync();
    }

    private Task<List<string>> RemindersFor(Booking booking) => ReminderStatesAsync(booking.Id.Value);
}
