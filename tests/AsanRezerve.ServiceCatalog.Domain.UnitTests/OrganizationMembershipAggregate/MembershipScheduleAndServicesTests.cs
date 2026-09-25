using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using DomainDayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.OrganizationMembershipAggregate;

/// <summary>
/// A member's working week and the services they perform belong to the MEMBERSHIP,
/// not to the person and not to the salon. That is what makes "Tue–Thu at Aria,
/// Fri–Sat at Pars" expressible for one person, and it is the last piece that made
/// a per-staff Provider record unnecessary.
///
/// <para>Both default to empty, and empty means "the salon's own hours" and "all of
/// the salon's services" — so a salon that does not distinguish between its staff
/// never has to fill either in.</para>
/// </summary>
public class MembershipScheduleAndServicesTests
{
    private static readonly ProviderId Organization = ProviderId.New();

    private static OrganizationMembership ServiceProvidingMember()
    {
        var m = OrganizationMembership.InviteExisting(UserId.CreateNew(), Organization);
        m.Accept();
        m.EnableStaffProfile();
        return m;
    }

    private static StaffWorkingDay Day(DomainDayOfWeek day, int fromHour, int toHour) =>
        StaffWorkingDay.Create(day, new TimeOnly(fromHour, 0), new TimeOnly(toHour, 0));

    #region Defaults

    [Fact]
    public void A_New_Member_Keeps_The_Salons_Hours_And_Performs_Everything()
    {
        var membership = ServiceProvidingMember();

        Assert.False(membership.StaffProfile!.HasOwnSchedule);
        Assert.Empty(membership.StaffProfile.WorkingDays);
        Assert.Empty(membership.StaffProfile.ServiceIds);

        // An empty assignment list means the member performs every service.
        Assert.True(membership.StaffProfile.PerformsService(Guid.NewGuid()));
    }

    #endregion

    #region Working schedule

    [Fact]
    public void Setting_Working_Days_Gives_The_Member_A_Schedule_Of_Their_Own()
    {
        var membership = ServiceProvidingMember();

        membership.SetWorkingSchedule(new[]
        {
            Day(DomainDayOfWeek.Tuesday, 10, 16),
            Day(DomainDayOfWeek.Wednesday, 10, 16)
        });

        Assert.True(membership.StaffProfile!.HasOwnSchedule);
        Assert.Equal(
            new[] { DomainDayOfWeek.Tuesday, DomainDayOfWeek.Wednesday },
            membership.StaffProfile.WorkingDays.Select(d => d.DayOfWeek).OrderBy(d => d));
    }

    [Fact]
    public void Setting_An_Empty_Schedule_Puts_The_Member_Back_On_The_Salons_Hours()
    {
        var membership = ServiceProvidingMember();
        membership.SetWorkingSchedule(new[] { Day(DomainDayOfWeek.Tuesday, 10, 16) });

        membership.SetWorkingSchedule(Array.Empty<StaffWorkingDay>());

        Assert.False(membership.StaffProfile!.HasOwnSchedule);
    }

    [Fact]
    public void A_Working_Day_Must_End_After_It_Starts()
    {
        Assert.Throws<DomainValidationException>(() => StaffWorkingDay.Create(
            DomainDayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void A_Member_Cannot_Be_Given_Two_Periods_On_The_Same_Day()
    {
        // Split shifts are not modelled. Silently keeping both rows would make
        // availability generation ambiguous, so this is refused rather than merged.
        var membership = ServiceProvidingMember();

        Assert.Throws<DomainValidationException>(() => membership.SetWorkingSchedule(new[]
        {
            Day(DomainDayOfWeek.Monday, 9, 12),
            Day(DomainDayOfWeek.Monday, 14, 18)
        }));
    }

    [Fact]
    public void A_Member_Who_Does_Not_Provide_Services_Has_No_Schedule_To_Set()
    {
        var manager = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), Organization, providesServices: false);

        Assert.Throws<DomainValidationException>(() =>
            manager.SetWorkingSchedule(new[] { Day(DomainDayOfWeek.Monday, 9, 17) }));
    }

    [Fact]
    public void A_Terminated_Member_Cannot_Have_Their_Schedule_Changed()
    {
        var membership = ServiceProvidingMember();
        membership.Terminate("left the salon");

        Assert.ThrowsAny<Exception>(() =>
            membership.SetWorkingSchedule(new[] { Day(DomainDayOfWeek.Monday, 9, 17) }));
    }

    #endregion

    #region Narrowing to the salon's hours

    [Fact]
    public void A_Members_Day_Is_Narrowed_To_The_Salons_Opening_Hours()
    {
        // The member says 08:00–20:00; the salon opens 09:00 and shuts at 18:00.
        var window = Day(DomainDayOfWeek.Monday, 8, 20)
            .IntersectWith(new TimeOnly(9, 0), new TimeOnly(18, 0));

        Assert.NotNull(window);
        Assert.Equal(new TimeOnly(9, 0), window!.Value.Start);
        Assert.Equal(new TimeOnly(18, 0), window.Value.End);
    }

    [Fact]
    public void A_Member_Working_Only_Inside_The_Salons_Hours_Keeps_Their_Own_Window()
    {
        var window = Day(DomainDayOfWeek.Monday, 12, 16)
            .IntersectWith(new TimeOnly(9, 0), new TimeOnly(18, 0));

        Assert.NotNull(window);
        Assert.Equal(new TimeOnly(12, 0), window!.Value.Start);
        Assert.Equal(new TimeOnly(16, 0), window.Value.End);
    }

    [Fact]
    public void A_Member_Whose_Hours_Fall_Entirely_Outside_The_Salons_Is_Not_Bookable_That_Day()
    {
        // Someone rostered 19:00–22:00 at a shop that shuts at 18:00 cannot be booked.
        Assert.Null(Day(DomainDayOfWeek.Monday, 19, 22)
            .IntersectWith(new TimeOnly(9, 0), new TimeOnly(18, 0)));
    }

    #endregion

    #region Service assignments

    [Fact]
    public void Assigning_Services_Restricts_The_Member_To_Those_Services()
    {
        var membership = ServiceProvidingMember();
        var haircut = Guid.NewGuid();
        var beard = Guid.NewGuid();
        var colouring = Guid.NewGuid();

        membership.SetServiceAssignments(new[] { haircut, beard });

        Assert.True(membership.StaffProfile!.PerformsService(haircut));
        Assert.True(membership.StaffProfile.PerformsService(beard));
        Assert.False(membership.StaffProfile.PerformsService(colouring));
    }

    [Fact]
    public void Clearing_The_Assignments_Restores_Performs_Everything()
    {
        var membership = ServiceProvidingMember();
        membership.SetServiceAssignments(new[] { Guid.NewGuid() });

        membership.SetServiceAssignments(Array.Empty<Guid>());

        Assert.Empty(membership.StaffProfile!.ServiceIds);
        Assert.True(membership.StaffProfile.PerformsService(Guid.NewGuid()));
    }

    [Fact]
    public void Repeated_Service_Ids_Are_Stored_Once()
    {
        var membership = ServiceProvidingMember();
        var haircut = Guid.NewGuid();

        membership.SetServiceAssignments(new[] { haircut, haircut });

        Assert.Single(membership.StaffProfile!.ServiceIds);
    }

    [Fact]
    public void A_Member_Who_Does_Not_Provide_Services_Has_No_Assignments_To_Set()
    {
        var manager = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), Organization, providesServices: false);

        Assert.Throws<DomainValidationException>(() =>
            manager.SetServiceAssignments(new[] { Guid.NewGuid() }));
    }

    #endregion

    #region Two salons, one person

    [Fact]
    public void One_Person_Can_Work_Different_Days_At_Two_Salons()
    {
        // The whole point of hanging the schedule off the membership: the same person
        // has two independent working weeks without two identities.
        var person = UserId.CreateNew();
        var aria = ProviderId.New();
        var pars = ProviderId.New();

        var atAria = OrganizationMembership.InviteExisting(person, aria);
        atAria.Accept();
        atAria.EnableStaffProfile();
        atAria.SetWorkingSchedule(new[]
        {
            Day(DomainDayOfWeek.Tuesday, 9, 17),
            Day(DomainDayOfWeek.Wednesday, 9, 17)
        });

        var atPars = OrganizationMembership.InviteExisting(person, pars);
        atPars.Accept();
        atPars.EnableStaffProfile();
        atPars.SetWorkingSchedule(new[] { Day(DomainDayOfWeek.Friday, 12, 20) });

        Assert.Equal(atAria.PersonId, atPars.PersonId);   // one person
        Assert.NotEqual(atAria.Id, atPars.Id);            // two memberships
        Assert.Equal(2, atAria.StaffProfile!.WorkingDays.Count);
        Assert.Equal(
            DomainDayOfWeek.Friday,
            Assert.Single(atPars.StaffProfile!.WorkingDays).DayOfWeek);
    }

    #endregion
}
