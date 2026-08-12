using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;
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
/// Accepting an invitation must leave the new member BOOKABLE — customers can pick them in the
/// booking flow and see their calendar.
///
/// <para>It did not. <c>InviteExisting</c> grants the StaffProvider role, but
/// <c>OrganizationMembership.ProvidesServices</c> requires the role AND an attached
/// <c>StaffProfile</c>, and nothing on the invitation path ever created one. Every accepted
/// member therefore had <c>ProvidesServices == false</c>, so <c>MemberBookabilityService.SyncAsync</c>
/// — which deliberately skips members who do not perform services — did nothing: no service
/// qualification, no generated availability. The colleague showed up on the roster and could never
/// be booked, with an empty calendar forever. <c>AddStaffToProvider</c>, the older way to add a
/// colleague, always called <c>EnableStaffProfile</c>, which is why staff added that way worked and
/// invited ones silently did not.</para>
///
/// <para>The keystone flow reports this as "member has NO slots (bookability was not
/// provisioned)". These tests pin the handler-level contract so it cannot regress without the
/// end-to-end script.</para>
/// </summary>
public class AcceptInvitationBookabilityTests
{
    private readonly IProviderInvitationReadRepository _invitationRead =
        Substitute.For<IProviderInvitationReadRepository>();
    private readonly IProviderInvitationWriteRepository _invitationWrite =
        Substitute.For<IProviderInvitationWriteRepository>();
    private readonly IOrganizationMembershipRepository _memberships =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IMembershipAuditRepository _audit =
        Substitute.For<IMembershipAuditRepository>();
    private readonly IPersonDirectory _people = Substitute.For<IPersonDirectory>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork =
        Substitute.For<IServiceCatalogUnitOfWork>();
    private readonly IMemberBookabilityService _bookability =
        Substitute.For<IMemberBookabilityService>();

    private readonly Provider _organization;
    private readonly Guid _inviteePersonId = Guid.NewGuid();
    private const string InviteePhone = "+989121110022";

    public AcceptInvitationBookabilityTests()
    {
        _organization = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()), "Salon", "desc", ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"),
            ProviderHierarchyType.Organization);

        // The invited phone resolves to the accepting caller — the security check the handler
        // performs before doing anything else.
        _people.FindByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PersonInfo(_inviteePersonId, "رضا", "قاسمی", InviteePhone, "Active"));

        _bookability.SyncAsync(Arg.Any<OrganizationMembership>(), Arg.Any<CancellationToken>())
            .Returns(new MemberBookabilityResult(1, 18));
    }

    private AcceptInvitationAsMemberCommandHandler CreateHandler()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, _inviteePersonId.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new AcceptInvitationAsMemberCommandHandler(
            _invitationRead, _invitationWrite, _memberships, _audit, _people, _unitOfWork,
            _bookability, accessor,
            Substitute.For<ILogger<AcceptInvitationAsMemberCommandHandler>>());
    }

    private ProviderInvitation PendingInvitation()
    {
        var invitation = ProviderInvitation.Create(
            _organization.Id, PhoneNumber.From(InviteePhone), "رضا قاسمی");
        _invitationRead.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task A_Newly_Accepted_Member_Is_Bookable()
    {
        // Arrange — first-time join, so the handler creates the membership itself.
        var invitation = PendingInvitation();
        _memberships.GetActiveByPersonAndOrganizationAsync(
                Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        OrganizationMembership? synced = null;
        await _bookability.SyncAsync(
            Arg.Do<OrganizationMembership>(m => synced = m), Arg.Any<CancellationToken>());
        _bookability.ClearReceivedCalls();

        // Act
        await CreateHandler().Handle(
            new AcceptInvitationAsMemberCommand(invitation.Id), CancellationToken.None);

        // Assert — bookability is synced, and the membership handed to it actually qualifies.
        // Asserting on ProvidesServices (not merely "SyncAsync was called") is the point: the bug
        // was that the call happened but was a no-op for a member with no staff profile.
        await _bookability.Received(1).SyncAsync(
            Arg.Any<OrganizationMembership>(), Arg.Any<CancellationToken>());
        synced.Should().NotBeNull();
        synced!.ProvidesServices.Should().BeTrue(
            "SyncAsync skips members who do not provide services, so without the staff profile " +
            "the member is never qualified and never gets availability");
        synced.StaffProfile.Should().NotBeNull("ProvidesServices needs the profile, not just the role");
        synced.Roles.Should().Contain(MembershipRole.StaffProvider);
    }

    [Fact]
    public async Task A_Rejoining_Member_Is_Bookable_Too()
    {
        // Arrange — an existing active membership is reused rather than duplicated.
        var invitation = PendingInvitation();
        var existing = OrganizationMembership.InviteExisting(
            UserId.From(_inviteePersonId), _organization.Id);
        existing.Accept();
        _memberships.GetActiveByPersonAndOrganizationAsync(
                Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        // Act
        await CreateHandler().Handle(
            new AcceptInvitationAsMemberCommand(invitation.Id), CancellationToken.None);

        // Assert
        existing.ProvidesServices.Should().BeTrue();
        await _bookability.Received(1).SyncAsync(existing, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Enabling the profile must not corrupt a member who already performs services — the
    /// handler runs on every acceptance, including a re-accept.
    /// </summary>
    [Fact]
    public async Task Accepting_Twice_Does_Not_Duplicate_The_Staff_Role()
    {
        var invitation = PendingInvitation();
        var existing = OrganizationMembership.InviteExisting(
            UserId.From(_inviteePersonId), _organization.Id);
        existing.Accept();
        existing.EnableStaffProfile("cuts");
        _memberships.GetActiveByPersonAndOrganizationAsync(
                Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        await CreateHandler().Handle(
            new AcceptInvitationAsMemberCommand(invitation.Id), CancellationToken.None);

        existing.ProvidesServices.Should().BeTrue();
        existing.Roles.Count(r => r == MembershipRole.StaffProvider).Should().Be(1);
        existing.StaffProfile!.BioOverride.Should().Be("cuts", "an existing bio must survive");
    }
}
