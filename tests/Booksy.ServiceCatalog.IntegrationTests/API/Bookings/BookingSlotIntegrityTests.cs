using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// C3 booking-slot-integrity: proves the database GiST exclusion constraint
/// (EXC_Bookings_Staff_NoOverlap) is the authoritative backstop against
/// double-booking — no overlapping ACTIVE booking for the same staff can ever
/// be committed, independent of the application read-check or availability rows.
///
/// Includes a randomized, high-concurrency invariant ("property") test: across
/// many seeds and heavy simultaneous contention with random overlapping ranges,
/// the invariant "no two active bookings for a staff overlap" must ALWAYS hold.
/// (A randomized concurrency invariant is the right tool here; FsCheck's pure
/// synchronous model does not fit concurrent async DB side-effects.)
/// </summary>
public class BookingSlotIntegrityTests : ServiceCatalogIntegrationTestBase
{
    public BookingSlotIntegrityTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Overlapping_active_bookings_for_same_staff_are_rejected_by_the_database()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var start = DateTime.UtcNow.AddDays(3).Date.AddHours(10);

        async Task InsertBooking(DateTime at, string note)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            db.Add(Booking.CreateBookingRequest(
                UserId.From(Guid.NewGuid()), provider.Id, service.Id, provider.Id,
                at, service.Duration, service.BasePrice, BookingPolicy.Default, note));
            await db.SaveChangesAsync();
        }

        await InsertBooking(start, "first");

        var overlap = () => InsertBooking(start, "overlap");
        (await overlap.Should().ThrowAsync<DbUpdateException>(
                "the database must reject overlapping active bookings for the same staff"))
            .Which.ToString().Should().Contain("EXC_Bookings_Staff_NoOverlap");

        var later = () => InsertBooking(start.AddDays(1), "later");
        await later.Should().NotThrowAsync("non-overlapping slots must remain bookable");
    }

    [Theory]
    [InlineData(101)]
    [InlineData(202)]
    [InlineData(303)]
    public async Task Stress_no_concurrent_combination_ever_creates_overlapping_active_bookings(int seed)
    {
        // Arrange — one staff (maximum contention) and a tight window so random
        // ranges heavily overlap.
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var window = DateTime.UtcNow.AddDays(20).Date.AddHours(9);
        var rng = new Random(seed);

        const int attempts = 40; // truly-simultaneous inserts (within the connection pool)
        var candidates = Enumerable.Range(0, attempts)
            .Select(_ => (
                at: window.AddMinutes(rng.Next(0, 180)),   // 3-hour window
                minutes: rng.Next(30, 91)))                 // 30–90 min durations → dense overlaps
            .ToList();

        async Task TryInsert((DateTime at, int minutes) c)
        {
            try
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
                db.Add(Booking.CreateBookingRequest(
                    UserId.From(Guid.NewGuid()), provider.Id, service.Id, provider.Id,
                    c.at, Duration.FromMinutes(c.minutes), service.BasePrice, BookingPolicy.Default, "stress"));
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Expected for losers: exclusion violation / serialization / deadlock.
                // The point is only that they did NOT commit an overlapping row.
            }
        }

        // Act — fire them all at once.
        await Task.WhenAll(candidates.Select(TryInsert));

        // Assert the invariant: no two committed ACTIVE bookings for this staff overlap.
        using var verify = Factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var active = await vdb.Set<Booking>()
            .Where(b => b.StaffId == provider.Id.Value &&
                        (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed))
            .Select(b => new { b.TimeSlot.StartTime, b.TimeSlot.EndTime })
            .ToListAsync();

        active.Should().NotBeEmpty("at least one booking must win the slot");
        for (var i = 0; i < active.Count; i++)
        {
            for (var j = i + 1; j < active.Count; j++)
            {
                var overlaps = active[i].StartTime < active[j].EndTime &&
                               active[i].EndTime > active[j].StartTime;
                overlaps.Should().BeFalse(
                    $"seed {seed}: committed active bookings [{active[i].StartTime:o},{active[i].EndTime:o}) and " +
                    $"[{active[j].StartTime:o},{active[j].EndTime:o}) must never overlap");
            }
        }
    }
}
