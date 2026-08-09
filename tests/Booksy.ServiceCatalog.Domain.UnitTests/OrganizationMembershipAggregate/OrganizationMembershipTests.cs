using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.OrganizationMembershipAggregate;

/// <summary>
/// Unit tests for the OrganizationMembership aggregate — the Person↔Organization link.
/// Cover the required real-world salon scenarios at the domain level:
///  S1 solo owner who provides services (owner is first staff, no invite),
///  S2 owner who only manages (no StaffProfile),
///  S3 owner who is also staff (both roles at once),
///  S5 changing salon (terminate + rejoin, identity preserved),
///  S6 working at multiple salons (many memberships, one person).
/// </summary>
public class OrganizationMembershipTests
{
    private readonly UserId _person = UserId.CreateNew();
    private readonly ProviderId _org = ProviderId.New();

    #region CreateOwner (S1, S2, S3)

    [Fact]
    public void CreateOwner_WhenProvidesServices_IsActiveOwnerAndStaffWithProfile()
    {
        // Arrange & Act — S1/S3: owner personally provides services
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: true);

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.True(membership.IsOwner);
        Assert.Contains(MembershipRole.Owner, membership.Roles);
        Assert.Contains(MembershipRole.StaffProvider, membership.Roles);
        Assert.NotNull(membership.StaffProfile);
        Assert.True(membership.ProvidesServices);
        Assert.NotNull(membership.JoinedAt);
    }

    [Fact]
    public void CreateOwner_WhenNotProvidingServices_HasOwnerRoleOnlyAndNoProfile()
    {
        // Arrange & Act — S2: owner only manages
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.True(membership.IsOwner);
        Assert.DoesNotContain(MembershipRole.StaffProvider, membership.Roles);
        Assert.Null(membership.StaffProfile);
        Assert.False(membership.ProvidesServices);
    }

    [Fact]
    public void CreateOwner_RaisesActivatedEvent()
    {
        // Act
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: true);

        // Assert
        var evt = Assert.Single(membership.DomainEvents.OfType<MembershipActivatedEvent>());
        Assert.Equal(membership.Id, evt.MembershipId);
        Assert.Equal(_org, evt.OrganizationId);
        Assert.Equal(_person.Value, evt.PersonId);
    }

    #endregion

    #region Invite + Accept (S4)

    [Fact]
    public void InviteExisting_StartsInvitedWithStaffRoleAndRaisesInvitedEvent()
    {
        // Act
        var membership = OrganizationMembership.InviteExisting(_person, _org);

        // Assert
        Assert.Equal(MembershipStatus.Invited, membership.Status);
        Assert.Contains(MembershipRole.StaffProvider, membership.Roles);
        Assert.NotNull(membership.InvitedAt);
        Assert.Null(membership.JoinedAt);
        Assert.Contains(membership.DomainEvents, e => e is MembershipInvitedEvent);
    }

    [Fact]
    public void Accept_ActivatesMembershipAndRaisesAcceptedEvent()
    {
        // Arrange
        var membership = OrganizationMembership.InviteExisting(_person, _org);

        // Act
        membership.Accept();

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.NotNull(membership.JoinedAt);
        var evt = Assert.Single(membership.DomainEvents.OfType<MembershipAcceptedEvent>());
        Assert.Equal(_person.Value, evt.PersonId);
    }

    [Fact]
    public void Accept_Throws_WhenNotInvited()
    {
        // Arrange
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => membership.Accept());
    }

    #endregion

    #region Terminate / Reinstate (S5)

    [Fact]
    public void Terminate_SetsTerminatedWithLeftAtAndReason()
    {
        // Arrange — S5: employee leaves salon
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: true);

        // Act
        membership.Terminate("moved away");

        // Assert
        Assert.Equal(MembershipStatus.Terminated, membership.Status);
        Assert.NotNull(membership.LeftAt);
        Assert.Equal("moved away", membership.TerminationReason);
        Assert.Contains(membership.DomainEvents, e => e is MembershipTerminatedEvent);
    }

    [Fact]
    public void Terminate_Throws_WhenAlreadyTerminated()
    {
        // Arrange
        var membership = OrganizationMembership.InviteExisting(_person, _org);
        membership.Terminate();

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => membership.Terminate());
    }

    [Fact]
    public void Reinstate_RestoresTerminatedMembershipToActive()
    {
        // Arrange
        var membership = OrganizationMembership.InviteExisting(_person, _org);
        membership.Accept();
        membership.Terminate("temporary leave");

        // Act — S5 rejoin (same membership object; the app normally creates a fresh one)
        membership.Reinstate();

        // Assert
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Null(membership.LeftAt);
        Assert.Null(membership.TerminationReason);
    }

    #endregion

    #region Multi-salon (S6)

    [Fact]
    public void SamePerson_CanHoldMembershipsInTwoOrganizations()
    {
        // Arrange & Act — S6: one person, two salons
        var orgA = ProviderId.New();
        var orgB = ProviderId.New();
        var membershipA = OrganizationMembership.InviteExisting(_person, orgA);
        var membershipB = OrganizationMembership.InviteExisting(_person, orgB);

        // Assert — one identity, two distinct memberships
        Assert.Equal(_person, membershipA.PersonId);
        Assert.Equal(_person, membershipB.PersonId);
        Assert.NotEqual(membershipA.Id, membershipB.Id);
        Assert.NotEqual(membershipA.OrganizationId, membershipB.OrganizationId);
    }

    #endregion

    #region Unclaimed members (staff without an app account)

    [Fact]
    public void An_Unclaimed_Member_Is_Active_And_Bookable_Without_A_Person()
    {
        // A salon adds a junior stylist who does not use the app.
        var membership = OrganizationMembership.CreateUnclaimed(_org, "سارا احمدی");

        Assert.Null(membership.PersonId);
        Assert.True(membership.IsUnclaimed);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.True(membership.ProvidesServices);      // bookable straight away
        Assert.Equal("سارا احمدی", membership.StaffProfile!.DisplayName);
    }

    [Fact]
    public void An_Unclaimed_Member_Requires_A_Name()
    {
        Assert.Throws<DomainValidationException>(
            () => OrganizationMembership.CreateUnclaimed(_org, "   "));
    }

    [Fact]
    public void Claiming_Attaches_A_Person_And_Preserves_The_Membership()
    {
        // The stylist later accepts an invitation on her phone: same membership,
        // same id — so her existing bookings and history survive.
        var membership = OrganizationMembership.CreateUnclaimed(_org, "سارا");
        var originalId = membership.Id;

        membership.ClaimBy(_person);

        Assert.Equal(_person, membership.PersonId);
        Assert.False(membership.IsUnclaimed);
        Assert.Equal(originalId, membership.Id);
        Assert.True(membership.ProvidesServices);
    }

    [Fact]
    public void A_Membership_That_Already_Has_A_Person_Cannot_Be_Claimed_Again()
    {
        var membership = OrganizationMembership.CreateUnclaimed(_org, "سارا");
        membership.ClaimBy(_person);

        Assert.Throws<DomainValidationException>(
            () => membership.ClaimBy(UserId.CreateNew()));
    }

    [Fact]
    public void A_Terminated_Membership_Cannot_Be_Claimed()
    {
        var membership = OrganizationMembership.CreateUnclaimed(_org, "سارا");
        membership.Terminate("left");

        Assert.Throws<DomainValidationException>(() => membership.ClaimBy(_person));
    }

    #endregion

    #region Roles & StaffProfile invariants

    [Fact]
    public void RemoveRole_Throws_WhenItWouldLeaveNoRoles()
    {
        // Arrange
        var membership = OrganizationMembership.InviteExisting(_person, _org); // {StaffProvider}

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => membership.RemoveRole(MembershipRole.StaffProvider));
    }

    [Fact]
    public void RemoveStaffProviderRole_AlsoRetiresStaffProfile()
    {
        // Arrange
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: true);

        // Act
        membership.RemoveRole(MembershipRole.StaffProvider);

        // Assert
        Assert.Null(membership.StaffProfile);
        Assert.False(membership.ProvidesServices);
        Assert.True(membership.IsOwner);
    }

    [Fact]
    public void EnableStaffProfile_AddsStaffRoleAndProfile()
    {
        // Arrange — manager who later starts providing services
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);

        // Act
        membership.EnableStaffProfile("cuts and colour");

        // Assert
        Assert.Contains(MembershipRole.StaffProvider, membership.Roles);
        Assert.NotNull(membership.StaffProfile);
        Assert.True(membership.ProvidesServices);
        Assert.Equal("cuts and colour", membership.StaffProfile!.BioOverride);
        Assert.Contains(membership.DomainEvents, e => e is StaffProfileEnabledEvent);
    }

    [Fact]
    public void ChangeRoles_ReplacesTheSet_AndSyncsStaffProfile()
    {
        // Arrange — owner-only membership.
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);

        // Act — make them Owner + StaffProvider.
        membership.ChangeRoles(new[] { MembershipRole.Owner, MembershipRole.StaffProvider });

        // Assert
        Assert.Contains(MembershipRole.Owner, membership.Roles);
        Assert.Contains(MembershipRole.StaffProvider, membership.Roles);
        Assert.NotNull(membership.StaffProfile);
        Assert.True(membership.ProvidesServices);

        // Act — drop back to Manager only; StaffProfile retires.
        membership.ChangeRoles(new[] { MembershipRole.Manager });

        // Assert
        Assert.Equal(new[] { MembershipRole.Manager }, membership.Roles);
        Assert.Null(membership.StaffProfile);
        Assert.False(membership.IsOwner);
    }

    [Fact]
    public void ChangeRoles_Throws_OnEmptySet()
    {
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);
        Assert.Throws<DomainValidationException>(() => membership.ChangeRoles(System.Array.Empty<MembershipRole>()));
    }

    [Fact]
    public void AssignRole_IsIdempotent()
    {
        // Arrange
        var membership = OrganizationMembership.CreateOwner(_person, _org, providesServices: false);
        var before = membership.Roles.Count;

        // Act
        membership.AssignRole(MembershipRole.Owner); // already present

        // Assert
        Assert.Equal(before, membership.Roles.Count);
    }

    [Fact]
    public void OperationsOnTerminatedMembership_AreRejected()
    {
        // Arrange
        var membership = OrganizationMembership.InviteExisting(_person, _org);
        membership.Terminate();

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => membership.AssignRole(MembershipRole.Manager));
        Assert.Throws<DomainValidationException>(() => membership.EnableStaffProfile());
    }

    #endregion
}
