using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;
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
/// No app-layer test existed for this handler before (only its domain method,
/// OrganizationMembership.ChangeRoles, was covered) -- these were added while fixing the
/// handler's authorization failure to throw ForbiddenException (403) instead of
/// UnauthorizedAccessException (401), which the controller's own XML docs already promised.
/// </summary>
public class ChangeMembershipRolesCommandHandlerTests
{
    private readonly IOrganizationMembershipRepository _membershipRepository =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IProviderReadRepository _providerRepository =
        Substitute.For<IProviderReadRepository>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork =
        Substitute.For<IServiceCatalogUnitOfWork>();
    private readonly IMembershipAuditRepository _audit =
        Substitute.For<IMembershipAuditRepository>();

    private ChangeMembershipRolesCommandHandler CreateHandler(Guid callerId)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, callerId.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new ChangeMembershipRolesCommandHandler(
            _membershipRepository,
            _audit,
            _providerRepository,
            _unitOfWork,
            accessor,
            Substitute.For<ILogger<ChangeMembershipRolesCommandHandler>>());
    }

    private static Provider CreateOrg(UserId owner) => Provider.RegisterProvider(
        owner,
        "Salon",
        "desc",
        ServiceCategory.Barbershop,
        ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
        BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

    private static OrganizationMembership ActiveStaff(UserId person, ProviderId org)
    {
        var m = OrganizationMembership.InviteExisting(person, org);
        m.Accept();
        return m;
    }

    [Fact]
    public async Task Owner_Can_Change_A_Members_Roles()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);
        var result = await handler.Handle(
            new ChangeMembershipRolesCommand(staff.Id, new[] { "Manager" }), CancellationToken.None);

        staff.Roles.Should().ContainSingle().Which.Should().Be(MembershipRole.Manager);
        result.Roles.Should().ContainSingle("Manager");
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Non_Owner_Cannot_Change_Roles()
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
            new ChangeMembershipRolesCommand(staff.Id, new[] { "Manager" }), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>(
            "authenticated but not permitted must be 403, not 401");
        staff.Roles.Should().ContainSingle().Which.Should().Be(MembershipRole.StaffProvider);
    }

    [Fact]
    public async Task Cannot_Demote_The_Last_Owner()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var ownerMembership = OrganizationMembership.CreateOwner(owner, org.Id, providesServices: false);

        _membershipRepository.GetByIdAsync(ownerMembership.Id, Arg.Any<CancellationToken>())
            .Returns(ownerMembership);
        _providerRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _membershipRepository.GetByOrganizationAsync(org.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { ownerMembership });

        var handler = CreateHandler(owner.Value);
        Func<Task> act = () => handler.Handle(
            new ChangeMembershipRolesCommand(ownerMembership.Id, new[] { "Manager" }), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().WithMessage("*last owner*");
        ownerMembership.Roles.Should().Contain(MembershipRole.Owner);
    }

    [Fact]
    public async Task Throws_When_Membership_Not_Found()
    {
        _membershipRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        var handler = CreateHandler(Guid.NewGuid());
        Func<Task> act = () => handler.Handle(
            new ChangeMembershipRolesCommand(Guid.NewGuid(), new[] { "Manager" }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Rejects_An_Unknown_Role_Name()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        var handler = CreateHandler(owner.Value);
        Func<Task> act = () => handler.Handle(
            new ChangeMembershipRolesCommand(staff.Id, new[] { "SuperAdmin" }), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().WithMessage("*Unknown role*");
    }
}
