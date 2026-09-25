using System.Net;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// If the salon never marks a visit done, the customer could never review it (request item 6). A confirmed booking
/// 12 hours past its end, not marked done or no-show, now completes by itself — through the real job, the real
/// command pipeline and the real database — and the customer is asked for a review and can write one
/// (openspec/changes/_inline/reviews-and-reschedule-round2 D3). The hosted service is off under test; the test runs
/// the job at the moment it chooses.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingCompletesByItselfTests : ReviewTestBase
{
    public BookingCompletesByItselfTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    /// <summary>A confirmed booking whose end is this far behind the salon's clock.</summary>
    private async Task<Visit> ConfirmedEndedAgoAsync(Domain.Aggregates.Provider provider, TimeSpan endedAgo)
    {
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var customerId = Guid.NewGuid();
        var start = SalonTime.Now - endedAgo - TimeSpan.FromMinutes(service.Duration.Value);
        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(customerId), provider.Id, service.Id, provider.Id.Value, start,
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "auto-complete test");
        await CreateEntityAsync(booking);
        return new Visit(booking.Id.Value, customerId, provider);
    }

    private async Task<int> RunTheJobAsync()
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AutoCompleteBookingsJob>().RunAsync(DateTime.UtcNow);
    }

    private Task<Booking> LoadBookingAsync(Guid id) =>
        FreshAsync(db => db.Bookings.AsNoTracking().Include(b => b.History).SingleAsync(b => b.Id == BookingId.From(id)));

    [Fact]
    public async Task Twelve_hours_after_its_end_an_unmarked_booking_completes_and_the_customer_can_review_it()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var due = await ConfirmedEndedAgoAsync(salon, BookingAutoCompletion.After + TimeSpan.FromMinutes(30));
        var notYet = await ConfirmedEndedAgoAsync(salon, BookingAutoCompletion.After - TimeSpan.FromHours(1));

        (await RunTheJobAsync()).Should().Be(1);

        var completed = await LoadBookingAsync(due.BookingId);
        completed.Status.Should().Be(BookingStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();
        completed.History.Should().Contain(h => h.Description == Booking.AutoCompletedHistoryEntry);
        (await LoadBookingAsync(notYet.BookingId)).Status.Should().Be(BookingStatus.Confirmed, "its 12 hours are not up");

        // Everything the salon's own «انجام شد» sets off — the review request above all.
        var asks = await FreshAsync(db => db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == due.BookingId)
            .Select(e => e.EventCode)
            .ToListAsync());
        asks.Should().Contain(new[] { NotificationEventCode.BookingCompleted, NotificationEventCode.ReviewRequest });

        var row = await MyBookingRowAsync(due.CustomerId, due.BookingId);
        row["canReview"]!.Value<bool>().Should().BeTrue();

        (await SubmitAsync(due, AReview())).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_second_pass_changes_nothing()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        await ConfirmedEndedAgoAsync(salon, BookingAutoCompletion.After + TimeSpan.FromHours(3));

        (await RunTheJobAsync()).Should().Be(1);
        (await RunTheJobAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_booking_the_salon_marked_no_show_stays_a_no_show()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var visit = await ConfirmedEndedAgoAsync(salon, BookingAutoCompletion.After + TimeSpan.FromHours(1));
        await FreshAsync(async db =>
        {
            var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId.From(visit.BookingId));
            booking.MarkAsNoShow("نیامد");
            await db.SaveChangesAsync();
            return 0;
        });

        (await RunTheJobAsync()).Should().Be(0);

        (await LoadBookingAsync(visit.BookingId)).Status.Should().Be(BookingStatus.NoShow);
    }

    [Fact]
    public async Task The_waiting_customer_is_told_the_visit_opens_by_itself()
    {
        var visit = await PastUncompletedVisitAsync();

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["reviewBlockedReason"]!.Value<string>().Should().Contain("۱۲ ساعت").And.Contain("خودکار");
    }
}
