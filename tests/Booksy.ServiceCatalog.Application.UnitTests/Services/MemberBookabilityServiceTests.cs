using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.Application.UnitTests.Services;

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
    private readonly MemberBookabilityService _sut;

    private readonly Provider _organization;

    public MemberBookabilityServiceTests()
    {
        _sut = new MemberBookabilityService(
            _providers, _services, _availability,
            Substitute.For<ILogger<MemberBookabilityService>>());

        _organization = Provider.RegisterProvider(
            UserId.CreateNew(),
            "Salon",
            "desc",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"),
            ProviderHierarchyType.Organization);

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
                Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderAvailability>());
    }

    private OrganizationMembership ServiceProvidingMember()
    {
        var m = OrganizationMembership.InviteExisting(UserId.CreateNew(), _organization.Id);
        m.Accept();
        m.EnableStaffProfile();
        return m;
    }

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
        _availability.FindOverlappingSlotsAsync(
                Arg.Any<ProviderId>(), Arg.Any<DateTime>(), Arg.Any<TimeOnly>(),
                Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderAvailability>
            {
                ProviderAvailability.CreateAvailable(
                    _organization.Id, DateTime.UtcNow.Date, new TimeOnly(9, 0),
                    new TimeOnly(9, 30), membership.Id)
            });

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().Be(0, "existing days are skipped — the sync is idempotent");
    }

    [Fact]
    public async Task A_Business_With_No_Open_Hours_Generates_Nothing()
    {
        var closed = Provider.RegisterProvider(
            UserId.CreateNew(), "Closed", "d", ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("c@t.com"), PhoneNumber.From("+989123456780")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"),
            ProviderHierarchyType.Organization);
        _providers.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>()).Returns(closed);

        var membership = ServiceProvidingMember();

        var result = await _sut.SyncAsync(membership);

        result.SlotsGenerated.Should().Be(0);
    }
}
