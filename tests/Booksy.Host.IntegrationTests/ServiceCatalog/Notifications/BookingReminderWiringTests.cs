using System.Net;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.IntegrationTests.API.Bookings;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

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
[Collection(BooksyHostTestCollection.Name)]
public class BookingReminderWiringTests : ServiceCatalogIntegrationTestBase
{
    public BookingReminderWiringTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cancelling_a_booking_through_the_api_withdraws_its_reminders()
    {
        var (customerId, booking) = await ArrangeBookingWithRemindersAsync();

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
        var (customerId, booking) = await ArrangeBookingWithRemindersAsync();

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
        var (customerId, cancelled) = await ArrangeBookingWithRemindersAsync();
        var (_, untouched) = await ArrangeBookingWithRemindersAsync();

        AuthenticateAsUser(customerId, "customer@test.com");
        await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{cancelled.Id.Value}/cancel",
            new CancelBookingRequest { Reason = "تغییر برنامه", CancelledBy = customerId });

        (await ReminderStatesAsync(untouched.Id.Value))
            .Should().OnlyContain(s => s == NotificationOutboxState.Pending);
    }

    // ── arrange ──

    private async Task<(Guid CustomerId, Booking Booking)> ArrangeBookingWithRemindersAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var customerId = Guid.NewGuid();
        var staffId = await GetBookableMemberIdAsync(provider);

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            staffId,
            DateTime.UtcNow.AddDays(3),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "reminder wiring");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var reminders = scope.ServiceProvider.GetRequiredService<IBookingReminderScheduler>();
            await reminders.ScheduleAsync(booking);
            await context.SaveChangesAsync();
        }

        return (customerId, booking);
    }

    private async Task<List<string>> ReminderStatesAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.SubjectType == BookingReminderScheduler.BookingSubject && e.SubjectId == bookingId)
            .Select(e => e.State)
            .ToListAsync();
    }

    private Task<List<string>> RemindersFor(Booking booking) => ReminderStatesAsync(booking.Id.Value);
}
