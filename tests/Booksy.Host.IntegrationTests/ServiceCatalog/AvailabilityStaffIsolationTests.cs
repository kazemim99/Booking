using Booksy.API;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Persistence;

/// <summary>
/// Availability slots are per-member, so looking them up has to be per-member too.
///
/// <para><c>FindOverlappingSlotsAsync</c> never filtered <c>ProviderAvailability.StaffId</c>:
/// it returned every overlapping slot in the ORGANIZATION regardless of which member owned
/// it. The two callers that only READ (the cancel/reschedule release paths) survive that,
/// because they re-check <c>slot.BookingId</c>. The two that WRITE do not — they mark every
/// overlapping Available slot as Booked, so booking one stylist at 10:00 consumed every
/// other stylist's 10:00 slot and the whole salon went dark after a single appointment.</para>
///
/// <para>This was invisible until per-member schedules landed, because until then only the
/// first member of a salon ever got slots generated at all (the sibling defect fixed in
/// <c>HasSlotsForStaffOnDateAsync</c>) — with one member, "all overlapping slots" and "this
/// member's slots" are the same set.</para>
///
/// <para>These run against real Postgres because the filter is SQL: an in-memory fake of the
/// repository would assert the mock, not the query.</para>
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class AvailabilityStaffIsolationTests : ServiceCatalogIntegrationTestBase
{
    public AvailabilityStaffIsolationTests(BooksyHostFactory factory)
        : base(factory)
    {
    }

    private IProviderAvailabilityWriteRepository Repository =>
        Scope.ServiceProvider.GetRequiredService<IProviderAvailabilityWriteRepository>();

    /// <summary>Tomorrow, so slot creation never trips the "no past dates" domain guard.</summary>
    private static DateTime Tomorrow => DateTime.UtcNow.Date.AddDays(1);

    private async Task<ProviderAvailability> GivenSlotAsync(
        ProviderId organizationId, Guid? staffId, int fromHour, int toHour)
    {
        var slot = ProviderAvailability.CreateAvailable(
            organizationId,
            Tomorrow,
            new TimeOnly(fromHour, 0),
            new TimeOnly(toHour, 0),
            staffId);

        await CreateEntityAsync(slot);
        return slot;
    }

    private async Task<IReadOnlyList<ProviderAvailability>> FindAsync(
        ProviderId organizationId, Guid? staffId, int fromHour, int toHour)
    {
        DbContext.ChangeTracker.Clear();

        return await Repository.FindOverlappingSlotsAsync(
            organizationId,
            Tomorrow,
            new TimeOnly(fromHour, 0),
            new TimeOnly(toHour, 0),
            excludeSlotId: null,
            staffId: staffId);
    }

    [Fact]
    public async Task Looking_Up_One_Members_Slots_Does_Not_Return_Another_Members()
    {
        // The core isolation failure: Alice and Bob both work 10:00-10:30 at the same salon.
        var organizationId = ProviderId.New();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var aliceSlot = await GivenSlotAsync(organizationId, alice, 10, 11);
        await GivenSlotAsync(organizationId, bob, 10, 11);

        var found = await FindAsync(organizationId, alice, 10, 11);

        found.Should().ContainSingle().Which.Id.Should().Be(aliceSlot.Id);
    }

    [Fact]
    public async Task Asking_For_No_Particular_Member_Still_Returns_Everyones_Slots()
    {
        // The organization-direct booking path relies on this, so narrowing the query for
        // members must not narrow it for the salon booked as a whole.
        var organizationId = ProviderId.New();
        await GivenSlotAsync(organizationId, Guid.NewGuid(), 10, 11);
        await GivenSlotAsync(organizationId, Guid.NewGuid(), 10, 11);

        var found = await FindAsync(organizationId, staffId: null, 10, 11);

        found.Should().HaveCount(2);
    }

    [Fact]
    public async Task A_Members_Lookup_Ignores_Slots_Of_A_Different_Salon()
    {
        // Slots are keyed by (organization, staff); neither half alone is enough.
        var mySalon = ProviderId.New();
        var otherSalon = ProviderId.New();
        var person = Guid.NewGuid();

        await GivenSlotAsync(otherSalon, person, 10, 11);

        var found = await FindAsync(mySalon, person, 10, 11);

        found.Should().BeEmpty();
    }

    [Fact]
    public async Task A_Member_With_No_Slots_Finds_Nothing_Even_When_Colleagues_Are_Free()
    {
        // What the marking path got wrong: a member who is not rostered at 10:00 must not
        // inherit a colleague's slot and appear bookable.
        var organizationId = ProviderId.New();
        await GivenSlotAsync(organizationId, Guid.NewGuid(), 10, 11);

        var found = await FindAsync(organizationId, Guid.NewGuid(), 10, 11);

        found.Should().BeEmpty();
    }

    [Fact]
    public async Task Booking_One_Member_Leaves_Their_Colleague_Bookable()
    {
        // The end consequence, stated as the business rule it is: a salon with two stylists
        // that takes one appointment at 10:00 still has one stylist free at 10:00.
        var organizationId = ProviderId.New();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        await GivenSlotAsync(organizationId, alice, 10, 11);
        var bobSlot = await GivenSlotAsync(organizationId, bob, 10, 11);

        // Exactly what MarkAvailabilityAsBookedAsync does, through the real repository.
        var bookingId = Guid.NewGuid();
        foreach (var slot in await FindAsync(organizationId, alice, 10, 11))
        {
            slot.MarkAsBooked(bookingId, nameof(AvailabilityStaffIsolationTests));
            await Repository.UpdateAsync(slot);
        }

        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var bobAfter = await DbContext.Set<ProviderAvailability>()
            .FirstAsync(s => s.Id == bobSlot.Id);

        bobAfter.Status.Should().Be(
            AvailabilityStatus.Available,
            "one stylist's appointment must not consume the whole salon's capacity");
    }

    [Fact]
    public async Task Excluding_A_Slot_Still_Works_Alongside_The_Staff_Filter()
    {
        // excludeSlotId is the parameter a staffId used to be passed into by mistake.
        // Both narrow the same query now, so prove they compose rather than collide.
        var organizationId = ProviderId.New();
        var alice = Guid.NewGuid();

        var first = await GivenSlotAsync(organizationId, alice, 10, 11);
        var second = await GivenSlotAsync(organizationId, alice, 10, 11);

        DbContext.ChangeTracker.Clear();
        var found = await Repository.FindOverlappingSlotsAsync(
            organizationId,
            Tomorrow,
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            excludeSlotId: first.Id,
            staffId: alice);

        found.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }
}
