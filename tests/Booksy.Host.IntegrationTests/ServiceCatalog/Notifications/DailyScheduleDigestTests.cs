using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.BackgroundJobs;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The salon is told each morning what its day looks like.
/// </summary>
/// <remarks>
/// <para>This is the one notification in the catalogue that nothing in the business causes. Every other
/// notification is raised by a command handler at the moment something happened; a digest is caused by the
/// morning arriving, so a job has to go looking for it.</para>
///
/// <para>That difference is why the count is computed at send time rather than captured at raise time. The
/// raiser's rule — capture the parameters when the thing happened — holds here too; it is just that for a
/// digest the thing that happens IS the count being taken. Raising the intent on the first booking of the
/// day and counting then would announce "you have 1 appointment" to a salon with nine.</para>
///
/// <para><b>Salon-local.</b> There is no provider timezone in this system: booking times are salon
/// wall-clock values (FOLLOW-UPS #63) stored in one frame. So "08:00 salon-local" is 08:00 in the same frame
/// the bookings already live in, which is what these tests assert. If #63 is ever resolved with a real
/// provider timezone, this becomes a conversion and these tests are where that shows up.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class DailyScheduleDigestTests : ServiceCatalogIntegrationTestBase
{
    public DailyScheduleDigestTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_day_with_bookings_tells_the_owner_how_many()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 3);

        await SweepAsync(Morning(day));

        var digest = (await RaisedForAsync(provider.OwnerId.Value)).Should().ContainSingle().Subject;
        digest.Code.Should().Be(NotificationEventCode.DailyScheduleDigest);
        digest.Count.Should().Be("3");
    }

    [Fact]
    public async Task The_timer_reads_eight_oclock_on_the_salons_clock()
    {
        // The job's own clock is UTC. 04:35 UTC is 08:05 at the salon: the digest is due. Read as-is it was
        // "04:35", before the morning, and the digest went out at 11:30 salon time (QA 2026-09-24).
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 2);

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(SalonTime.ToUtc(Morning(day)));
        using (var scope = Factory.Services.CreateScope())
        {
            var job = new DailyScheduleDigestJob(
                scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>(),
                scope.ServiceProvider.GetRequiredService<INotificationRaiser>(),
                clock,
                NullLogger<DailyScheduleDigestJob>.Instance);
            await job.ExecuteAsync(CancellationToken.None);
        }

        var digest = (await RaisedForAsync(provider.OwnerId.Value)).Should().ContainSingle().Subject;
        digest.Count.Should().Be("2");
    }

    [Fact]
    public async Task A_day_with_no_bookings_says_nothing()
    {
        // Decided with the user 2026-09-20: skip the send rather than report zero. "You have 0 appointments
        // today" is a notification whose only content is that there was nothing to notify about.
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();

        await SweepAsync(Morning(day));

        (await RaisedForAsync(provider.OwnerId.Value)).Should().BeEmpty();
    }

    [Fact]
    public async Task Running_the_sweep_twice_tells_them_once()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 2);

        // The sweep runs on a timer, so the same morning is seen many times. Once is the whole point.
        await SweepAsync(Morning(day));
        await SweepAsync(Morning(day).AddMinutes(15));
        await SweepAsync(Morning(day).AddMinutes(30));

        (await RaisedForAsync(provider.OwnerId.Value)).Should().ContainSingle();
    }

    [Fact]
    public async Task Nothing_goes_out_before_the_morning()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 2);

        await SweepAsync(day.AddHours(7).AddMinutes(45));

        (await RaisedForAsync(provider.OwnerId.Value)).Should().BeEmpty(
            "the digest is a morning message, not a midnight one");
    }

    [Fact]
    public async Task Nothing_goes_out_once_the_day_is_half_gone()
    {
        // A host that restarts at lunchtime must not then announce a schedule the salon has already worked
        // through. The day is skipped instead: late is not better than silent for this one.
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 2);

        await SweepAsync(day.AddHours(16));

        (await RaisedForAsync(provider.OwnerId.Value)).Should().BeEmpty();
    }

    [Fact]
    public async Task Cancelled_bookings_are_not_counted()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        var bookings = await SeedConfirmedBookingsAsync(provider, day, count: 3);
        await CancelAsync(bookings[0]);

        await SweepAsync(Morning(day));

        var digest = (await RaisedForAsync(provider.OwnerId.Value)).Should().ContainSingle().Subject;
        digest.Count.Should().Be("2", "a cancelled appointment is not on the day's schedule");
    }

    [Fact]
    public async Task A_request_still_awaiting_a_decision_is_not_counted()
    {
        // A request is not yet an appointment. Counting it would tell the salon it has work booked that it
        // has not agreed to do.
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 1);
        await SeedRequestedBookingAsync(provider, day.AddHours(13));

        await SweepAsync(Morning(day));

        var digest = (await RaisedForAsync(provider.OwnerId.Value)).Should().ContainSingle().Subject;
        digest.Count.Should().Be("1");
    }

    [Fact]
    public async Task Each_day_gets_its_own_digest()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 1);
        await SeedConfirmedBookingsAsync(provider, day.AddDays(1), count: 1);

        await SweepAsync(Morning(day));
        await SweepAsync(Morning(day.AddDays(1)));

        (await RaisedForAsync(provider.OwnerId.Value)).Should().HaveCount(
            2, "de-duplication is per day, not forever");
    }

    [Fact]
    public async Task The_digest_goes_to_the_owner_not_the_provider_id()
    {
        // The fifth place in this change where addressing a ProviderId would produce a notification nobody
        // could ever see. Asserted rather than remembered.
        var provider = await CreateTestProviderWithServicesAsync();
        var day = PickDay();
        await SeedConfirmedBookingsAsync(provider, day, count: 1);

        await SweepAsync(Morning(day));

        (await RaisedForAsync(provider.Id.Value)).Should().BeEmpty(
            "a provider id addresses nobody — the inbox is keyed by user");
    }

    // ── arrange ──

    /// <summary>08:05 — a few minutes after the sweep's opening moment, as a real timer pass would be.</summary>
    private static DateTime Morning(DateTime day) => day.AddHours(8).AddMinutes(5);

    /// <summary>
    /// The day these tests use. It does not need to be unique per test: the job sweeps every salon with
    /// bookings that morning, but each test has its own provider and every assertion is scoped to that
    /// provider's owner, so another test's salon sharing the day is invisible here.
    /// </summary>
    private static DateTime PickDay() => DateTime.UtcNow.Date.AddDays(200);

    private async Task<List<Booking>> SeedConfirmedBookingsAsync(
        Domain.Aggregates.Provider provider, DateTime day, int count)
    {
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();
        var bookings = new List<Booking>();

        for (var i = 0; i < count; i++)
        {
            var booking = Booking.CreateConfirmedByProvider(
                UserId.From(Guid.NewGuid()),
                provider.Id,
                service.Id,
                provider.Id.Value,
                day.AddHours(9 + i),
                service.Duration,
                service.BasePrice,
                service.BookingPolicy ?? BookingPolicy.Default,
                "digest");

            await CreateEntityAsync(booking);
            bookings.Add(booking);
        }

        return bookings;
    }

    private async Task SeedRequestedBookingAsync(Domain.Aggregates.Provider provider, DateTime start)
    {
        var service = (await GetProviderServicesAsync(provider.Id.Value)).First();

        var booking = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            provider.Id.Value,
            start,
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "awaiting a decision");

        await CreateEntityAsync(booking);
    }

    private async Task CancelAsync(Booking booking)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var tracked = await db.Bookings.FirstAsync(b => b.Id == booking.Id);
        tracked.Cancel("customer changed their mind");
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Runs one pass of the digest job at an explicit moment. The job takes the time as an argument rather
    /// than reading a clock, because "08:00 salon-local" is the entire behaviour under test and a test that
    /// could only observe it at 08:00 real time would never run.
    /// </summary>
    private async Task SweepAsync(DateTime now)
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<DailyScheduleDigestJob>();
        await job.RunAsync(now, CancellationToken.None);
    }

    private sealed record Raised(NotificationEventCode Code, string? Count);

    private async Task<List<Raised>> RaisedForAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var rows = await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.RecipientId == recipientId)
            .Select(e => new { e.EventCode, e.ParametersJson })
            .ToListAsync();

        return rows.Select(r => new Raised(r.EventCode, CountOf(r.ParametersJson))).ToList();
    }

    private static string? CountOf(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
            return null;

        using var document = System.Text.Json.JsonDocument.Parse(parametersJson);
        return document.RootElement.TryGetProperty("count", out var value) ? value.GetString() : null;
    }
}
