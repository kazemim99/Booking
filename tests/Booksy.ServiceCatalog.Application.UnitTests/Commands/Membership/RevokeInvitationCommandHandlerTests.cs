using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;
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
/// Revoking closes a pending invitation. It is owner-only, and the attempt is kept
/// in the audit trail rather than deleted.
/// </summary>
public class RevokeInvitationCommandHandlerTests
{
    private readonly IProviderInvitationReadRepository _invitationRead =
        Substitute.For<IProviderInvitationReadRepository>();
    private readonly IProviderInvitationWriteRepository _invitationWrite =
        Substitute.For<IProviderInvitationWriteRepository>();
    private readonly IOrganizationMembershipRepository _memberships =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IProviderReadRepository _providers =
        Substitute.For<IProviderReadRepository>();
    private readonly IMembershipAuditRepository _audit =
        Substitute.For<IMembershipAuditRepository>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork =
        Substitute.For<IServiceCatalogUnitOfWork>();

    private readonly Provider _organization;
    private readonly UserId _owner = UserId.From(Guid.NewGuid());

    public RevokeInvitationCommandHandlerTests()
    {
        _organization = Provider.RegisterProvider(
            _owner, "Salon", "desc", ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"),
            ProviderHierarchyType.Organization);

        _providers.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(_organization);
        _memberships.GetActiveByPersonAndOrganizationAsync(
                Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);
    }

    private RevokeInvitationCommandHandler CreateHandler(Guid callerId)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, callerId.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new RevokeInvitationCommandHandler(
            _invitationRead, _invitationWrite, _memberships, _providers, _audit, _unitOfWork,
            accessor, Substitute.For<ILogger<RevokeInvitationCommandHandler>>());
    }

    private ProviderInvitation PendingInvitation()
    {
        var invitation = ProviderInvitation.Create(
            _organization.Id, PhoneNumber.From("+989121110022"), "سارا");
        _invitationRead.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task Owner_Revokes_A_Pending_Invitation_And_It_Is_Audited()
    {
        var invitation = PendingInvitation();

        var result = await CreateHandler(_owner.Value)
            .Handle(new RevokeInvitationCommand(invitation.Id, "wrong number"), CancellationToken.None);

        invitation.Status.Should().Be(InvitationStatus.Cancelled);
        result.Status.Should().Be("Cancelled");

        await _audit.Received(1).AppendAsync(
            Arg.Is<MembershipAuditEntry>(e =>
                e.InvitationId == invitation.Id &&
                e.MembershipId == null &&                 // no membership ever existed
                e.Action == MembershipAuditAction.InvitationRevoked &&
                e.ActorPersonId!.Value == _owner.Value &&
                e.Reason == "wrong number"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Non_Owner_Cannot_Revoke()
    {
        var invitation = PendingInvitation();

        Func<Task> act = () => CreateHandler(Guid.NewGuid())
            .Handle(new RevokeInvitationCommand(invitation.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        invitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public async Task An_Already_Accepted_Invitation_Cannot_Be_Revoked()
    {
        var invitation = PendingInvitation();
        invitation.AcceptByMember();

        Func<Task> act = () => CreateHandler(_owner.Value)
            .Handle(new RevokeInvitationCommand(invitation.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().WithMessage("*pending*");
    }

    [Fact]
    public async Task Throws_When_The_Invitation_Does_Not_Exist()
    {
        _invitationRead.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProviderInvitation?)null);

        Func<Task> act = () => CreateHandler(_owner.Value)
            .Handle(new RevokeInvitationCommand(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
