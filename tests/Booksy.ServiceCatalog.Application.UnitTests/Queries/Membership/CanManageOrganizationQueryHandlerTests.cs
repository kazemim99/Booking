using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.UnitTests.Queries.Membership;

/// <summary>
/// Who may act on a salon, and in what capacity.
///
/// <para>The controllers used to answer this with "does this person OWN this provider?",
/// in three separate copies. Under the membership model that is wrong in both directions:
/// an employed manager can legitimately run a salon they do not own, and a stylist who
/// owns nothing was refused on every provider-scoped route — which is what left the
/// provider app blank for anyone but a salon owner.</para>
/// </summary>
public class CanManageOrganizationQueryHandlerTests
{
    private readonly IOrganizationMembershipRepository _memberships =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IProviderReadRepository _providers = Substitute.For<IProviderReadRepository>();

    private static readonly UserId OwnerId = UserId.CreateNew();
    private readonly Provider _organization;

    public CanManageOrganizationQueryHandlerTests()
    {
        _organization = Provider.RegisterProvider(
            OwnerId,
            "Salon",
            "desc",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

        _providers.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(_organization);
    }

    private CanManageOrganizationQueryHandler HandlerFor(UserId caller)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, caller.Value.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new CanManageOrganizationQueryHandler(_memberships, _providers, accessor);
    }

    private OrganizationMembership MemberWith(UserId personId, params MembershipRole[] roles)
    {
        var membership = OrganizationMembership.InviteExisting(personId, _organization.Id);
        membership.Accept();
        foreach (var role in roles)
            membership.AssignRole(role);

        _memberships.GetActiveByPersonAndOrganizationAsync(
                personId, _organization.Id, Arg.Any<CancellationToken>())
            .Returns(membership);

        return membership;
    }

    private Task<bool> AskAsync(UserId caller, OrganizationPermission permission) =>
        HandlerFor(caller).Handle(
            new CanManageOrganizationQuery(_organization.Id.Value, permission),
            CancellationToken.None);

    #region Running the salon

    [Fact]
    public async Task An_Owner_Member_May_Manage_The_Organization()
    {
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.Owner);

        (await AskAsync(person, OrganizationPermission.ManageOrganization)).Should().BeTrue();
    }

    [Fact]
    public async Task An_Employed_Manager_May_Manage_A_Salon_They_Do_Not_Own()
    {
        // The whole point of separating ownership from membership: running a salon is a
        // role you can be given, not something only the person on the deed can do.
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.Manager);

        (await AskAsync(person, OrganizationPermission.ManageOrganization)).Should().BeTrue();
        person.Should().NotBe(_organization.OwnerId);
    }

    [Fact]
    public async Task A_Stylist_May_Not_Rewrite_The_Salons_Settings()
    {
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.StaffProvider);

        (await AskAsync(person, OrganizationPermission.ManageOrganization)).Should().BeFalse();
    }

    [Fact]
    public async Task A_Receptionist_May_Not_Rewrite_The_Salons_Settings()
    {
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.Receptionist);

        (await AskAsync(person, OrganizationPermission.ManageOrganization)).Should().BeFalse();
    }

    #endregion

    #region The day book

    [Fact]
    public async Task A_Stylist_May_See_The_Salons_Bookings()
    {
        // The regression this whole change exists for: an employed stylist owns no
        // Provider, so the owner-only check refused them and their app showed nothing.
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.StaffProvider);

        (await AskAsync(person, OrganizationPermission.ManageBookings)).Should().BeTrue();
    }

    [Fact]
    public async Task A_Receptionist_May_See_The_Salons_Bookings()
    {
        var person = UserId.CreateNew();
        MemberWith(person, MembershipRole.Receptionist);

        (await AskAsync(person, OrganizationPermission.ManageBookings)).Should().BeTrue();
    }

    #endregion

    #region Outsiders

    [Fact]
    public async Task Someone_With_No_Membership_May_Do_Nothing()
    {
        var stranger = UserId.CreateNew();
        _memberships.GetActiveByPersonAndOrganizationAsync(
                stranger, _organization.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        (await AskAsync(stranger, OrganizationPermission.ManageOrganization)).Should().BeFalse();
        (await AskAsync(stranger, OrganizationPermission.ManageBookings)).Should().BeFalse();
    }

    [Fact]
    public async Task A_Member_Of_A_Different_Salon_May_Do_Nothing_Here()
    {
        // A person can work at several salons; a membership at one grants nothing at another.
        var person = UserId.CreateNew();
        _memberships.GetActiveByPersonAndOrganizationAsync(
                person, _organization.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        (await AskAsync(person, OrganizationPermission.ManageBookings)).Should().BeFalse();
    }

    [Fact]
    public async Task A_Terminated_Member_May_Do_Nothing()
    {
        // GetActiveByPersonAndOrganizationAsync excludes terminated memberships, which is
        // what makes "they left last month" actually revoke access.
        var person = UserId.CreateNew();
        _memberships.GetActiveByPersonAndOrganizationAsync(
                person, _organization.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        (await AskAsync(person, OrganizationPermission.ManageBookings)).Should().BeFalse();
    }

    [Fact]
    public async Task An_Unauthenticated_Caller_May_Do_Nothing()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        accessor.HttpContext.Returns(httpContext);

        var handler = new CanManageOrganizationQueryHandler(_memberships, _providers, accessor);

        var allowed = await handler.Handle(
            new CanManageOrganizationQuery(
                _organization.Id.Value, OrganizationPermission.ManageBookings),
            CancellationToken.None);

        allowed.Should().BeFalse();
    }

    #endregion

    #region The migration fallback

    [Fact]
    public async Task A_Legacy_Owner_With_No_Membership_Row_Still_Has_Full_Access()
    {
        // Provider.OwnerId is the fallback for organizations registered before ownership
        // moved onto memberships. An owner with no membership row is a gap in the
        // backfill, not a downgrade in what they may do.
        _memberships.GetActiveByPersonAndOrganizationAsync(
                OwnerId, _organization.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        (await AskAsync(OwnerId, OrganizationPermission.ManageOrganization)).Should().BeTrue();
        (await AskAsync(OwnerId, OrganizationPermission.ManageBookings)).Should().BeTrue();
    }

    #endregion
}
