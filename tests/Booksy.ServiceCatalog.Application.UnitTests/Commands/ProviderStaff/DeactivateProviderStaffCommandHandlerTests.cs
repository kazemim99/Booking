using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Provider.DeactivateProviderStaff;
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

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderStaff;

/// <summary>
/// Covers the legacy Vue admin route DELETE /api/v1/Providers/{id}/staff/{staffId}.
///
/// This handler's file was previously commented out in its entirety, so MediatR had no handler
/// registered for <see cref="DeactivateProviderStaffCommand"/> and the endpoint threw
/// InvalidOperationException — a 500 on a destructive admin action. The first test below is the
/// regression guard: it fails with a resolution error if the handler is ever removed again.
///
/// The `staffId` the route receives is mixed by design (see GetProviderStaffQueryHandler): a
/// MembershipId for membership rows, a legacy sub-provider's ProviderId for un-migrated ones.
/// Both paths are covered.
/// </summary>
public class DeactivateProviderStaffCommandHandlerTests
{
    private readonly IOrganizationMembershipRepository _membershipRepository =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IMembershipAuditRepository _audit =
        Substitute.For<IMembershipAuditRepository>();
    private readonly IProviderReadRepository _providerReadRepository =
        Substitute.For<IProviderReadRepository>();
    private readonly IProviderWriteRepository _providerWriteRepository =
        Substitute.For<IProviderWriteRepository>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork =
        Substitute.For<IServiceCatalogUnitOfWork>();

    private DeactivateProviderStaffCommandHandler CreateHandler(Guid callerId)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, callerId.ToString()) })));
        accessor.HttpContext.Returns(httpContext);

        return new DeactivateProviderStaffCommandHandler(
            _membershipRepository,
            _audit,
            _providerReadRepository,
            _providerWriteRepository,
            _unitOfWork,
            accessor,
            Substitute.For<ILogger<DeactivateProviderStaffCommandHandler>>());
    }

    private static Provider CreateOrg(UserId owner) =>
        Provider.RegisterProvider(
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
    public async Task Owner_Can_Remove_A_Staff_Member_By_MembershipId()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerReadRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);
        var result = await handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, staff.Id, "Removed from provider"),
            CancellationToken.None);

        result.IsActive.Should().BeFalse();
        result.StaffId.Should().Be(staff.Id);
        staff.Status.Should().Be(MembershipStatus.Terminated);
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Removing_The_Last_Owner_Is_Refused()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);

        var ownerMembership = OrganizationMembership.InviteExisting(owner, org.Id);
        ownerMembership.Accept();
        ownerMembership.ChangeRoles(new[] { MembershipRole.Owner });

        _membershipRepository.GetByIdAsync(ownerMembership.Id, Arg.Any<CancellationToken>())
            .Returns(ownerMembership);
        _providerReadRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _membershipRepository.GetByOrganizationAsync(org.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationMembership> { ownerMembership });

        var handler = CreateHandler(owner.Value);

        var act = () => handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, ownerMembership.Id, "bye"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*last owner*");
        ownerMembership.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task A_Stranger_Cannot_Remove_Staff()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerReadRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(Guid.NewGuid()); // neither the owner nor the member

        var act = () => handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, staff.Id, "nope"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        staff.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task Membership_Belonging_To_Another_Organization_Is_Not_Found()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var otherOrg = CreateOrg(UserId.From(Guid.NewGuid()));
        var staffElsewhere = ActiveStaff(UserId.From(Guid.NewGuid()), otherOrg.Id);

        _membershipRepository.GetByIdAsync(staffElsewhere.Id, Arg.Any<CancellationToken>())
            .Returns(staffElsewhere);
        _providerReadRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);

        var act = () => handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, staffElsewhere.Id, "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        staffElsewhere.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task Removing_An_Already_Terminated_Membership_Is_A_No_Op()
    {
        var owner = UserId.From(Guid.NewGuid());
        var org = CreateOrg(owner);
        var staff = ActiveStaff(UserId.From(Guid.NewGuid()), org.Id);
        staff.Terminate("already gone");

        _membershipRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _providerReadRepository.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var handler = CreateHandler(owner.Value);
        var result = await handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, staff.Id, "again"),
            CancellationToken.None);

        // A retried DELETE must stay successful rather than turning into an error.
        result.IsActive.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_Provider_Is_Not_Found()
    {
        var org = CreateOrg(UserId.From(Guid.NewGuid()));
        _providerReadRepository.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((Provider?)null);

        var handler = CreateHandler(Guid.NewGuid());

        var act = () => handler.Handle(
            new DeactivateProviderStaffCommand(org.Id.Value, Guid.NewGuid(), "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
