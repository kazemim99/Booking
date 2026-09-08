using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Membership.UpdateMembership;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.Membership;

/// <summary>
/// Replaces the dead <c>UpdateProviderStaffCommandHandler</c> (commented out in its entirety,
/// so <c>PUT /Providers/{id}/staff/{staffId}</c> and the staff-photo upload both threw
/// "handler not found" on every call — FOLLOW-UPS #16).
///
/// <para>The important domain rule here: a salon may edit its own view of a member (display
/// name while they have no account, per-salon bio and photo, whether they take bookings) but
/// may NOT rewrite the identity of a person who has their own account. The legacy contract
/// sent first/last name for every member and conflated the two.</para>
/// </summary>
public class UpdateMembershipCommandHandlerTests
{
    private readonly IOrganizationMembershipRepository _memberships = Substitute.For<IOrganizationMembershipRepository>();
    private readonly IMembershipAuditRepository _audit = Substitute.For<IMembershipAuditRepository>();
    private readonly IProviderReadRepository _providers = Substitute.For<IProviderReadRepository>();
    private readonly IMemberBookabilityService _bookability = Substitute.For<IMemberBookabilityService>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork = Substitute.For<IServiceCatalogUnitOfWork>();

    private static readonly UserId OwnerId = UserId.CreateNew();
    private static readonly UserId MemberId = UserId.CreateNew();
    private static readonly UserId StrangerId = UserId.CreateNew();

    private static Provider CreateOrg() =>
        Provider.RegisterProvider(
            OwnerId,
            "Salon",
            "desc",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

    private UpdateMembershipCommandHandler CreateHandler(UserId caller, OrganizationMembership membership, Provider org)
    {
        _memberships.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
        _providers.GetByIdAsync(membership.OrganizationId, Arg.Any<CancellationToken>()).Returns(org);

        var ownerMembership = OrganizationMembership.CreateOwner(OwnerId, org.Id, providesServices: false);
        _memberships.GetActiveByPersonAndOrganizationAsync(OwnerId, org.Id, Arg.Any<CancellationToken>())
            .Returns(ownerMembership);

        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, caller.Value.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new UpdateMembershipCommandHandler(
            _memberships, _audit, _providers, _bookability, _unitOfWork, accessor,
            Substitute.For<ILogger<UpdateMembershipCommandHandler>>());
    }

    private static OrganizationMembership Unclaimed(Provider org) =>
        OrganizationMembership.CreateUnclaimed(org.Id, "Sara Stylist");

    private static OrganizationMembership Claimed(Provider org)
    {
        var m = OrganizationMembership.InviteExisting(MemberId, org.Id);
        m.Accept();
        m.EnableStaffProfile();
        return m;
    }

    [Fact]
    public async Task Owner_Can_Rename_A_Member_Who_Has_No_Account()
    {
        var org = CreateOrg();
        var membership = Unclaimed(org);
        var handler = CreateHandler(OwnerId, membership, org);

        var result = await handler.Handle(
            new UpdateMembershipCommand(membership.Id, DisplayName: "Sara Ahmadi"), CancellationToken.None);

        result.DisplayName.Should().Be("Sara Ahmadi");
        membership.StaffProfile!.DisplayName.Should().Be("Sara Ahmadi");
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_Cannot_Rename_A_Member_Who_Has_Their_Own_Account()
    {
        // Their name belongs to their Person record, not to the salon.
        var org = CreateOrg();
        var membership = Claimed(org);
        var handler = CreateHandler(OwnerId, membership, org);

        Func<Task> act = () => handler.Handle(
            new UpdateMembershipCommand(membership.Id, DisplayName: "Someone Else"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Bio_And_Photo_Are_Per_Salon_And_Editable_For_A_Claimed_Member()
    {
        var org = CreateOrg();
        var membership = Claimed(org);
        var handler = CreateHandler(OwnerId, membership, org);

        var result = await handler.Handle(
            new UpdateMembershipCommand(membership.Id, BioOverride: "Senior stylist", PhotoUrl: "/img/sara.jpg"),
            CancellationToken.None);

        result.BioOverride.Should().Be("Senior stylist");
        result.PhotoUrl.Should().Be("/img/sara.jpg");
    }

    [Fact]
    public async Task Turning_Provides_Services_On_Makes_The_Member_Bookable()
    {
        var org = CreateOrg();
        var membership = OrganizationMembership.InviteExisting(MemberId, org.Id);
        membership.Accept(); // active, but not a service provider yet
        var handler = CreateHandler(OwnerId, membership, org);

        var result = await handler.Handle(
            new UpdateMembershipCommand(membership.Id, ProvidesServices: true), CancellationToken.None);

        result.ProvidesServices.Should().BeTrue();
        result.Roles.Should().Contain(nameof(MembershipRole.StaffProvider));
        await _bookability.Received(1).SyncAsync(membership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Turning_Provides_Services_Off_Stops_Bookability_Without_Removing_The_Member()
    {
        var org = CreateOrg();
        var membership = Claimed(org);
        membership.AssignRole(MembershipRole.Receptionist); // keeps a role after StaffProvider goes
        var handler = CreateHandler(OwnerId, membership, org);

        var result = await handler.Handle(
            new UpdateMembershipCommand(membership.Id, ProvidesServices: false), CancellationToken.None);

        result.ProvidesServices.Should().BeFalse();
        membership.Status.Should().Be(MembershipStatus.Active);
        await _bookability.DidNotReceive().SyncAsync(Arg.Any<OrganizationMembership>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Member_Can_Update_Their_Own_Membership()
    {
        var org = CreateOrg();
        var membership = Claimed(org);
        _memberships.GetActiveByPersonAndOrganizationAsync(MemberId, org.Id, Arg.Any<CancellationToken>())
            .Returns(membership);
        var handler = CreateHandler(MemberId, membership, org);

        var result = await handler.Handle(
            new UpdateMembershipCommand(membership.Id, BioOverride: "I specialise in fades"),
            CancellationToken.None);

        result.BioOverride.Should().Be("I specialise in fades");
    }

    [Fact]
    public async Task A_Stranger_Cannot_Update_Someone_Elses_Membership()
    {
        var org = CreateOrg();
        var membership = Claimed(org);
        _memberships.GetActiveByPersonAndOrganizationAsync(StrangerId, org.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);
        var handler = CreateHandler(StrangerId, membership, org);

        Func<Task> act = () => handler.Handle(
            new UpdateMembershipCommand(membership.Id, BioOverride: "hacked"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Updating_A_Missing_Membership_Is_A_NotFound()
    {
        var org = CreateOrg();
        var membership = Claimed(org);
        var handler = CreateHandler(OwnerId, membership, org);
        _memberships.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        Func<Task> act = () => handler.Handle(
            new UpdateMembershipCommand(Guid.NewGuid(), BioOverride: "x"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
