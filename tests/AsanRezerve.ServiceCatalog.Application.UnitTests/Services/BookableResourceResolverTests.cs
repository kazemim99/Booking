using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Services;

/// <summary>
/// What a booking's <c>StaffId</c> resolves to, and — the part that had no test at all — how
/// that resource's availability slots are keyed.
///
/// <para><c>SlotStaffId</c> is what stops one member's booking consuming a colleague's slot.
/// It is null for the organization booked directly (the salon owns all of its capacity) and
/// the membership id for a member. Getting those two the wrong way round silently either
/// blocks the whole salon or releases nothing, so both are pinned here.</para>
/// </summary>
public class BookableResourceResolverTests
{
    private readonly IOrganizationMembershipRepository _memberships =
        Substitute.For<IOrganizationMembershipRepository>();
    private readonly IProviderReadRepository _providers = Substitute.For<IProviderReadRepository>();
    private readonly BookableResourceResolver _sut;

    private readonly Provider _organization;

    public BookableResourceResolverTests()
    {
        _sut = new BookableResourceResolver(_memberships, _providers);

        _organization = Provider.RegisterProvider(
            UserId.CreateNew(),
            "Salon",
            "desc",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));
    }

    private OrganizationMembership GivenBookableMember()
    {
        var membership = OrganizationMembership.InviteExisting(UserId.CreateNew(), _organization.Id);
        membership.Accept();
        membership.EnableStaffProfile();

        _memberships.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
        return membership;
    }

    [Fact]
    public async Task A_Member_Keys_Their_Slots_By_Their_Membership_Id()
    {
        var membership = GivenBookableMember();

        var resource = await _sut.ResolveAsync(_organization, membership.Id, requireBookable: true);

        resource.Kind.Should().Be(BookableResourceKind.Member);
        resource.SlotOwnerId.Should().Be(_organization.Id, "a member's slots hang off the salon");
        resource.SlotStaffId.Should().Be(
            membership.Id, "which is what narrows slot lookups to this member alone");
    }

    [Fact]
    public async Task The_Organization_Booked_Directly_Keys_No_Staff_At_All()
    {
        // Null here means "do not narrow" — the salon booked as a whole owns every slot it
        // has. If this ever became the organization's own id it would match no slot's
        // StaffId, and direct bookings would silently mark and release nothing.
        var resource = await _sut.ResolveAsync(
            _organization, _organization.Id.Value, requireBookable: true);

        resource.Kind.Should().Be(BookableResourceKind.Organization);
        resource.SlotStaffId.Should().BeNull();
    }

    [Fact]
    public async Task Two_Members_Of_One_Salon_Get_Different_Slot_Keys()
    {
        // The isolation property stated directly: same salon, same slot owner, different key.
        var alice = GivenBookableMember();
        var bob = GivenBookableMember();

        var aliceResource = await _sut.ResolveAsync(_organization, alice.Id, requireBookable: true);
        var bobResource = await _sut.ResolveAsync(_organization, bob.Id, requireBookable: true);

        aliceResource.SlotOwnerId.Should().Be(bobResource.SlotOwnerId);
        aliceResource.SlotStaffId.Should().Be(alice.Id);
        bobResource.SlotStaffId.Should().Be(bob.Id);
        aliceResource.SlotStaffId.Should().NotBe(bobResource.SlotStaffId!.Value);
    }

    [Fact]
    public async Task A_Member_Of_Another_Salon_Is_Rejected()
    {
        var membership = OrganizationMembership.InviteExisting(UserId.CreateNew(), ProviderId.New());
        membership.Accept();
        membership.EnableStaffProfile();
        _memberships.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var act = () => _sut.ResolveAsync(_organization, membership.Id, requireBookable: true);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task A_Member_Who_Does_Not_Provide_Services_Is_Not_Bookable()
    {
        var membership = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), _organization.Id, providesServices: false);
        _memberships.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var act = () => _sut.ResolveAsync(_organization, membership.Id, requireBookable: true);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rescheduling_Does_Not_Re_Litigate_Bookability()
    {
        // An existing booking's resource may since have stopped taking new clients; moving
        // that booking to a new time must still work.
        var membership = OrganizationMembership.CreateOwner(
            UserId.CreateNew(), _organization.Id, providesServices: false);
        _memberships.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var resource = await _sut.ResolveAsync(_organization, membership.Id, requireBookable: false);

        resource.SlotStaffId.Should().Be(membership.Id);
    }

    [Fact]
    public async Task An_Unknown_Id_Is_Not_Found()
    {
        // There is no third branch any more: since the provider hierarchy was removed, a
        // staff reference is this salon or one of its memberships, and nothing else.
        _memberships.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);

        var act = () => _sut.ResolveAsync(_organization, Guid.NewGuid(), requireBookable: true);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
