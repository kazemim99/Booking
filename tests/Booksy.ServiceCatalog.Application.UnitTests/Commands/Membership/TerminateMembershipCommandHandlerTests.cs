using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;
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

public class TerminateMembershipCommandHandlerTests
{
    private readonly IOrganizationMembershipRepository _membershipRepository =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IProviderReadRepository _providerRepository =
        Substitute.For<IProviderReadRepository>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork =
        Substitute.For<IServiceCatalogUnitOfWork>();
    private readonly IMembershipAuditRepository _audit =
        Substitute.For<IMembershipAuditRepository>();

    private TerminateMembershipCommandHandler CreateHandler(Guid callerId)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, callerId.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new TerminateMembershipCommandHandler(
            _membershipRepository,
            _audit,
            _providerRepository,
            _unitOfWork,
            accessor,
            Substitute.For<ILogger<TerminateMembershipCommandHandler>>());
    }

    private static Provider CreateOrg(UserId owner) => Provider.RegisterProvider(
        owner,
        "Salon",
        "desc",
        ServiceCategory.Barbershop,
        ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
        BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"),
        ProviderHierarchyType.Organization);

    private static OrganizationMembership ActiveStaff(UserId person, ProviderId org)
    {
        var m = OrganizationMembership.InviteExisting(person, org);
        m.Accept();
        return m;
    }

    [Fact]
    public async Task Owner_Can_Terminate_A_Staff_Member()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);
        var result = await handler.Handle(new TerminateMembershipCommand(staff.Id, "let go"), CancellationToken.None);

        staff.Status.Should().Be(MembershipStatus.Terminated);
        result.MembershipId.Should().Be(staff.Id);
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Member_Can_Leave_Their_Own_Membership()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var me = UserId.From(Guid.NewGuid());
        var staff = ActiveStaff(me, org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        var handler = CreateHandler(me.Value);
        await handler.Handle(new TerminateMembershipCommand(staff.Id, "leaving"), CancellationToken.None);

        staff.Status.Should().Be(MembershipStatus.Terminated);
    }

    [Fact]
    public async Task Cannot_Remove_The_Last_Owner()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var ownerMembership = OrganizationMembership.CreateOwner(owner, org.Id, providesServices: false);

        _membershipRepository.GetByIdAsync(ownerMembership.Id, Arg.Any<CancellationToken>()).Returns(ownerMembership);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _membershipRepository.GetByOrganizationAsync(org.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { ownerMembership });

        var handler = CreateHandler(owner.Value);
        Func<Task> act = () => handler.Handle(
            new TerminateMembershipCommand(ownerMembership.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().WithMessage("*last owner*");
        ownerMembership.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task A_Stranger_Cannot_Terminate_A_Membership()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        var handler = CreateHandler(Guid.NewGuid()); // unrelated caller
        Func<Task> act = () => handler.Handle(
            new TerminateMembershipCommand(staff.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        staff.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task Termination_Is_Recorded_In_The_Audit_Trail()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);
        await handler.Handle(new TerminateMembershipCommand(staff.Id, "moved away"), CancellationToken.None);

        await _audit.Received(1).AppendAsync(
            Arg.Is<MembershipAuditEntry>(e =>
                e.MembershipId == staff.Id &&
                e.Action == MembershipAuditAction.Terminated &&
                e.StatusAfter == MembershipStatus.Terminated &&
                e.ActorPersonId!.Value == owner.Value &&
                e.Reason == "moved away"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Membership_Not_Found()
    {
        _membershipRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        var handler = CreateHandler(Guid.NewGuid());
        Func<Task> act = () => handler.Handle(
            new TerminateMembershipCommand(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
