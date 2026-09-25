using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using DomainDayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Services;

/// <summary>
/// A member becomes a bookable resource the moment they join: qualified for the
/// organization's services, with availability keyed by MembershipId. No shadow
/// "staff provider" record is ever created.
/// </summary>
public class MemberBookabilityServiceTests
{
    private readonly IProviderReadRepository _providers = Substitute.For<IProviderReadRepository>();
    private readonly IServiceWriteRepository _services = Substitute.For<IServiceWriteRepository>();
    private readonly IProviderAvailabilityWriteRepository _availability =
        Substitute.For<IProviderAvailabilityWriteRepository>();
    private readonly IOrganizationMembershipRepository _memberships =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly MemberBookabilityService _sut;

    private readonly Provider _organization;

    public MemberBookabilityServiceTests()
    {
        _sut = new MemberBookabilityService(
            _providers, _services, _availability, _memberships,
            Substitute.For<ILogger<MemberBookabilityService>>());

        _organization = Provider.RegisterProvider(
            UserId.CreateNew(),
            "Salon",
            "desc",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

        // Open every day 09:00–18:00 so the rolling window always has hours.
        var hours = new Dictionary<DomainDayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();
        foreach (DomainDayOfWeek day in Enum.GetValues<DomainDayOfWeek>())
            hours[day] = (new TimeOnly(9, 0), new TimeOnly(18, 0));
        _organization.SetBusinessHours(hours);

        _providers.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(_organization);
        _services.GetServicesByProviderIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Service>());
        _availability.FindOverlappingSlotsAsync(
                Arg.Any<ProviderId>(), Arg.Any<DateTime>(), Arg.Any<TimeOnly>(),
                Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ProviderAvailability>());
        _availability.HasSlotsForStaffOnDateAsync(
                Arg.Any<ProviderId>(), Arg.Any<DateTime>(), Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private OrganizationMembership ServiceProvidingMember()
    {
        var m = OrganizationMembership.InviteExisting(UserId.CreateNew(), _organization.Id);
        m.Accept();
        m.EnableStaffProfile();
        return m;
    }

    private static Service DraftService(ProviderId organizationId) =>
        Service.Create(
            organizationId,
            "Haircut",
            "desc",
            ServiceCategory.Barbershop,
            ServiceType.Standard,
            Price.Create(500_000m, "IRR"),
            Duration.FromMinutes(30));

    [Fact]
    public async Task A_Member_Who_Does_Not_Provide_Services_Is_Not_A_Bookable_Resource()
    {
        // Manager-only membership: present in the team, absent from booking.
        var membership = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), _organization.Id, providesServices: false);

        var result = await _sut.SyncAsync(membership);

        result.Should().Be(MemberBookabilityResult.None);
        await _availability.DidNotReceive().SaveAsync(
            Arg.Any<ProviderAvailability>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_Invited_But_Not_Yet_Accepted_Member_Is_Not_Bookable()
    {
        var membership = OrganizationMembership.InviteExisting(UserId.CreateNew(), _organization.Id);
        membership.EnableStaffProfile(); // still Invited, not Active

        var result = await _sut.SyncAsync(membership);

        result.Should().Be(MemberBookabilityResult.None);
    }

    [Fact]
    public async Task An_Active_Member_Gets_Availability_Keyed_By_MembershipId_Owned_By_The_Organization()
    {
        var membership = ServiceProvidingMember();
        var saved = new List<ProviderAvailability>();
        await _availability.SaveAsync(
            Arg.Do<ProviderAvailability>(saved.Add), Arg.Any<CancellationToken>());

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().BeGreaterThan(0);
        saved.Should().NotBeEmpty();
        saved.Should().OnlyContain(s =>
            s.StaffId == membership.Id &&        // the member IS the resource
            s.ProviderId == _organization.Id);   // the slot belongs to the business
    }

    [Fact]
    public async Task Repeating_The_Sync_Does_Not_Duplicate_Availability()
    {
        var membership = ServiceProvidingMember();

        // Second run: every day already has slots for this member.
        _availability.HasSlotsForStaffOnDateAsync(
                Arg.Any<ProviderId>(), Arg.Any<DateTime>(), membership.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().Be(0, "existing days are skipped — the sync is idempotent");
    }

    [Fact]
    public async Task A_Second_Member_Of_The_Same_Salon_Still_Gets_Their_Own_Availability()
    {
        // Regression: the idempotency check used to ask "does this DAY have slots?" for the
        // whole organization (it passed the membership id into the excludeSlotId parameter).
        // Once the first member was synced every later member generated ZERO slots and was
        // silently unbookable — a salon could only ever book its first employee.
        var first = ServiceProvidingMember();
        var second = ServiceProvidingMember();

        _availability.HasSlotsForStaffOnDateAsync(
                Arg.Any<ProviderId>(), Arg.Any<DateTime>(), first.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.SyncAsync(second);

        result.SlotsGenerated.Should().BeGreaterThan(
            0, "the second member's availability is independent of the first member's");
    }

    #region The member's own working week

    [Fact]
    public async Task A_Member_With_Their_Own_Schedule_Only_Gets_Slots_On_The_Days_They_Work()
    {
        // The salon opens every day; this member works Tuesdays only.
        var membership = ServiceProvidingMember();
        membership.SetWorkingSchedule(new[]
        {
            StaffWorkingDay.Create(DomainDayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(16, 0))
        });

        var saved = new List<ProviderAvailability>();
        await _availability.SaveAsync(
            Arg.Do<ProviderAvailability>(saved.Add), Arg.Any<CancellationToken>());

        await _sut.SyncAsync(membership);

        saved.Should().NotBeEmpty();
        saved.Should().OnlyContain(s => s.Date.DayOfWeek == System.DayOfWeek.Tuesday);
    }

    [Fact]
    public async Task A_Members_Hours_Are_Narrowed_To_The_Salons_Opening_Hours()
    {
        // The member claims 06:00–22:00; the salon runs 09:00–18:00. Nobody is bookable
        // while the shop is shut, so the generated slots live inside the overlap.
        var membership = ServiceProvidingMember();
        membership.SetWorkingSchedule(
            Enum.GetValues<DomainDayOfWeek>()
                .Select(d => StaffWorkingDay.Create(d, new TimeOnly(6, 0), new TimeOnly(22, 0)))
                .ToList());

        var saved = new List<ProviderAvailability>();
        await _availability.SaveAsync(
            Arg.Do<ProviderAvailability>(saved.Add), Arg.Any<CancellationToken>());

        await _sut.SyncAsync(membership);

        saved.Should().NotBeEmpty();
        saved.Should().OnlyContain(s =>
            s.StartTime >= new TimeOnly(9, 0) && s.EndTime <= new TimeOnly(18, 0));
    }

    [Fact]
    public async Task A_Member_Rostered_Only_Outside_The_Salons_Hours_Gets_Nothing()
    {
        // 19:00–22:00 at a shop that shuts at 18:00: no overlap, so no slots at all.
        var membership = ServiceProvidingMember();
        membership.SetWorkingSchedule(
            Enum.GetValues<DomainDayOfWeek>()
                .Select(d => StaffWorkingDay.Create(d, new TimeOnly(19, 0), new TimeOnly(22, 0)))
                .ToList());

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().Be(0);
    }

    [Fact]
    public async Task Regenerating_Clears_The_Members_Free_Slots_First()
    {
        // A schedule edit has to undo the availability the OLD schedule produced —
        // otherwise the already-generated days keep serving the old roster forever.
        var membership = ServiceProvidingMember();

        await _sut.SyncAsync(membership, regenerateAvailability: true);

        await _availability.Received(1).RemoveFreeStaffSlotsFromAsync(
            _organization.Id, membership.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_Ordinary_Sync_Never_Removes_Existing_Slots()
    {
        var membership = ServiceProvidingMember();

        await _sut.SyncAsync(membership);

        await _availability.DidNotReceive().RemoveFreeStaffSlotsFromAsync(
            Arg.Any<ProviderId>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Service assignments

    [Fact]
    public async Task A_Member_With_No_Assignments_Is_Qualified_For_Every_Service()
    {
        var membership = ServiceProvidingMember();
        var haircut = DraftService(_organization.Id);
        var colouring = DraftService(_organization.Id);
        _services.GetServicesByProviderIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Service> { haircut, colouring });

        var result = await _sut.SyncAsync(membership);

        result.ServicesQualified.Should().Be(2);
        haircut.IsStaffQualified(membership.Id).Should().BeTrue();
        colouring.IsStaffQualified(membership.Id).Should().BeTrue();
    }

    [Fact]
    public async Task A_Member_Assigned_To_One_Service_Is_Only_Qualified_For_That_One()
    {
        var membership = ServiceProvidingMember();
        var haircut = DraftService(_organization.Id);
        var colouring = DraftService(_organization.Id);
        _services.GetServicesByProviderIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Service> { haircut, colouring });

        membership.SetServiceAssignments(new[] { haircut.Id.Value });

        var result = await _sut.SyncAsync(membership);

        result.ServicesQualified.Should().Be(1);
        haircut.IsStaffQualified(membership.Id).Should().BeTrue();
        colouring.IsStaffQualified(membership.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Narrowing_The_Assignments_Withdraws_The_Member_From_The_Dropped_Service()
    {
        // Assignments have to be able to shrink, not only grow: a barber who stops doing
        // colouring must stop being offered for it.
        var membership = ServiceProvidingMember();
        var haircut = DraftService(_organization.Id);
        var colouring = DraftService(_organization.Id);
        _services.GetServicesByProviderIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Service> { haircut, colouring });

        await _sut.SyncAsync(membership);              // performs everything
        membership.SetServiceAssignments(new[] { haircut.Id.Value });
        await _sut.SyncAsync(membership);              // now haircuts only

        haircut.IsStaffQualified(membership.Id).Should().BeTrue();
        colouring.IsStaffQualified(membership.Id).Should().BeFalse();
    }

    #endregion

    #region A newly added service

    [Fact]
    public async Task A_New_Service_Is_Qualified_And_Activated_By_The_Salons_Members()
    {
        // Service.Create leaves a service in Draft with nobody qualified, and Activate()
        // refuses to run without a qualified member. Before SyncServiceAsync existed, only
        // membership events qualified anyone — so a salon could add a service, see it in
        // its own list, and never be able to sell it.
        var alice = ServiceProvidingMember();
        var bob = ServiceProvidingMember();
        _memberships.GetByOrganizationAsync(_organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { alice, bob });

        var service = DraftService(_organization.Id);

        var qualified = await _sut.SyncServiceAsync(service);

        qualified.Should().Be(2);
        service.Status.Should().Be(ServiceStatus.Active);
        service.CanBeBooked().Should().BeTrue();
    }

    [Fact]
    public async Task A_New_Service_Skips_Members_Who_Do_Not_Perform_It()
    {
        var generalist = ServiceProvidingMember();
        var specialist = ServiceProvidingMember();
        var service = DraftService(_organization.Id);

        // The specialist performs only some OTHER service.
        specialist.SetServiceAssignments(new[] { Guid.NewGuid() });

        _memberships.GetByOrganizationAsync(_organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { generalist, specialist });

        var qualified = await _sut.SyncServiceAsync(service);

        qualified.Should().Be(1);
        service.IsStaffQualified(generalist.Id).Should().BeTrue();
        service.IsStaffQualified(specialist.Id).Should().BeFalse();
    }

    [Fact]
    public async Task A_New_Service_Ignores_Members_Who_Do_Not_Provide_Services()
    {
        var manager = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), _organization.Id, providesServices: false);
        _memberships.GetByOrganizationAsync(_organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { manager });

        var service = DraftService(_organization.Id);

        var qualified = await _sut.SyncServiceAsync(service);

        qualified.Should().Be(0);
        service.Status.Should().Be(ServiceStatus.Draft,
            "a service nobody can perform must not be offered to customers");
    }

    [Fact]
    public async Task Syncing_The_Same_Service_Twice_Changes_Nothing()
    {
        var member = ServiceProvidingMember();
        _memberships.GetByOrganizationAsync(_organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { member });

        var service = DraftService(_organization.Id);
        await _sut.SyncServiceAsync(service);

        var second = await _sut.SyncServiceAsync(service);

        second.Should().Be(0);
        service.QualifiedStaff.Should().ContainSingle();
        service.Status.Should().Be(ServiceStatus.Active);
    }

    #endregion

    [Fact]
    public async Task A_Business_With_No_Open_Hours_Generates_Nothing()
    {
        var closed = Provider.RegisterProvider(
            UserId.CreateNew(), "Closed", "d", ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("c@t.com"), PhoneNumber.From("+989123456780")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));
        _providers.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>()).Returns(closed);

        var membership = ServiceProvidingMember();

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().Be(0);
    }
}
