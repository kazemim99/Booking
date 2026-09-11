using Booksy.API;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.IntegrationTests.Persistence;

/// <summary>
/// A member's working week and service assignments have to SURVIVE a save/load, against
/// the real Postgres schema. They are the first owned collection hanging off another owned
/// entity (StaffProfile) in this context, and the service ids ride in a jsonb column — both
/// are the kind of mapping that compiles, passes every in-memory unit test, and then throws
/// or silently loses data on the first real round trip.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class MembershipSchedulePersistenceTests : ServiceCatalogIntegrationTestBase
{
    public MembershipSchedulePersistenceTests(BooksyHostFactory factory)
        : base(factory)
    {
    }

    private static OrganizationMembership ServiceProvidingMember(ProviderId organizationId)
    {
        var m = OrganizationMembership.InviteExisting(UserId.CreateNew(), organizationId);
        m.Accept();
        m.EnableStaffProfile();
        return m;
    }

    private async Task<OrganizationMembership> ReloadAsync(Guid membershipId)
    {
        // The context is long-lived across a test, so its identity map would hand back the
        // instance we just wrote rather than what Postgres actually stored.
        DbContext.ChangeTracker.Clear();

        return await DbContext.Set<OrganizationMembership>()
            .FirstAsync(m => m.Id == membershipId);
    }

    [Fact]
    public async Task A_Members_Working_Week_Survives_A_Round_Trip()
    {
        var membership = ServiceProvidingMember(ProviderId.New());
        membership.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(16, 0)),
            StaffWorkingDay.Create(DomainDayOfWeek.Friday, new TimeOnly(12, 30), new TimeOnly(20, 0))
        });

        DbContext.Add(membership);
        await DbContext.SaveChangesAsync();

        var reloaded = await ReloadAsync(membership.Id);

        reloaded.StaffProfile.Should().NotBeNull();
        reloaded.StaffProfile!.HasOwnSchedule.Should().BeTrue();
        reloaded.StaffProfile.WorkingDays.Should().HaveCount(2);

        var friday = reloaded.StaffProfile.WorkingDays
            .Single(d => d.DayOfWeek == DomainDayOfWeek.Friday);
        friday.StartTime.Should().Be(new TimeOnly(12, 30));
        friday.EndTime.Should().Be(new TimeOnly(20, 0));
    }

    [Fact]
    public async Task Replacing_The_Working_Week_Does_Not_Leave_The_Old_Days_Behind()
    {
        // SetWorkingDays clears and re-adds, which is where an owned collection typically
        // either orphans rows or throws a concurrency error on the phantom UPDATE.
        var membership = ServiceProvidingMember(ProviderId.New());
        membership.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            StaffWorkingDay.Create(DomainDayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0))
        });
        DbContext.Add(membership);
        await DbContext.SaveChangesAsync();

        var toEdit = await ReloadAsync(membership.Id);
        toEdit.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Saturday, new TimeOnly(11, 0), new TimeOnly(15, 0))
        });
        await DbContext.SaveChangesAsync();

        var reloaded = await ReloadAsync(membership.Id);

        reloaded.StaffProfile!.WorkingDays.Should().ContainSingle()
            .Which.DayOfWeek.Should().Be(DomainDayOfWeek.Saturday);
    }

    [Fact]
    public async Task Clearing_The_Working_Week_Puts_The_Member_Back_On_The_Salons_Hours()
    {
        var membership = ServiceProvidingMember(ProviderId.New());
        membership.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0))
        });
        DbContext.Add(membership);
        await DbContext.SaveChangesAsync();

        var toEdit = await ReloadAsync(membership.Id);
        toEdit.SetWorkingSchedule(Array.Empty<StaffWorkingDay>());
        await DbContext.SaveChangesAsync();

        var reloaded = await ReloadAsync(membership.Id);

        reloaded.StaffProfile!.WorkingDays.Should().BeEmpty();
        reloaded.StaffProfile.HasOwnSchedule.Should().BeFalse();
    }

    [Fact]
    public async Task Service_Assignments_Survive_A_Round_Trip()
    {
        var haircut = Guid.NewGuid();
        var beard = Guid.NewGuid();

        var membership = ServiceProvidingMember(ProviderId.New());
        membership.SetServiceAssignments(new[] { haircut, beard });
        DbContext.Add(membership);
        await DbContext.SaveChangesAsync();

        var reloaded = await ReloadAsync(membership.Id);

        reloaded.StaffProfile!.ServiceIds.Should().BeEquivalentTo(new[] { haircut, beard });
        reloaded.StaffProfile.PerformsService(haircut).Should().BeTrue();
        reloaded.StaffProfile.PerformsService(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public async Task A_Member_With_No_Schedule_Or_Assignments_Round_Trips_As_Empty_Not_Null()
    {
        // The default and by far the commonest case: everyone works the shop's hours and
        // performs everything. It must come back as an empty jsonb array, never null.
        var membership = ServiceProvidingMember(ProviderId.New());
        DbContext.Add(membership);
        await DbContext.SaveChangesAsync();

        var reloaded = await ReloadAsync(membership.Id);

        reloaded.StaffProfile!.WorkingDays.Should().BeEmpty();
        reloaded.StaffProfile.ServiceIds.Should().BeEmpty();
        reloaded.StaffProfile.PerformsService(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public async Task Two_Memberships_Of_One_Person_Keep_Separate_Working_Weeks()
    {
        // The invariant the whole redesign exists for: one identity, two schedules.
        var person = UserId.CreateNew();
        var aria = ProviderId.New();
        var pars = ProviderId.New();

        var atAria = OrganizationMembership.InviteExisting(person, aria);
        atAria.Accept();
        atAria.EnableStaffProfile();
        atAria.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0))
        });

        var atPars = OrganizationMembership.InviteExisting(person, pars);
        atPars.Accept();
        atPars.EnableStaffProfile();
        atPars.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Friday, new TimeOnly(12, 0), new TimeOnly(20, 0))
        });

        DbContext.AddRange(atAria, atPars);
        await DbContext.SaveChangesAsync();

        var reloadedAria = await ReloadAsync(atAria.Id);
        var reloadedPars = await ReloadAsync(atPars.Id);

        reloadedAria.StaffProfile!.WorkingDays.Single().DayOfWeek
            .Should().Be(DomainDayOfWeek.Tuesday);
        reloadedPars.StaffProfile!.WorkingDays.Single().DayOfWeek
            .Should().Be(DomainDayOfWeek.Friday);
    }
}
