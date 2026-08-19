using System.Reflection;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ServiceAggregate;

/// <summary>
/// Unit tests for the rule that decides whether a service can ever be offered to a customer:
/// <c>Service.Activate()</c> requires an organization's service to have at least one qualified staff member.
///
/// <para>This is the invariant that made the whole seeded catalogue unbookable — 70 services sat in
/// <see cref="ServiceStatus.Draft"/> because no staff existed to qualify them, and the availability engine
/// refuses to generate slots for a non-Active service. These tests pin down the contract the seeders (and
/// <c>MemberBookabilityService</c>) rely on: qualify a member first, then activate. They exist so the rule is
/// never "fixed" by weakening it into letting staff-less organization services go live.</para>
/// </summary>
public class ServiceActivationTests
{
    private static Service NewDraftService(ProviderId? providerId = null) =>
        Service.Create(
            providerId ?? ProviderId.New(),
            "Men's Haircut - کوتاهی مو مردانه",
            "اصلاح و آرایش موی مردانه",
            ServiceCategory.Barbershop,
            ServiceType.Standard,
            Price.Create(500_000m, "IRR"),
            Duration.FromMinutes(30));

    #region The staff requirement

    [Fact]
    public void Create_ProducesADraftServiceWithNoQualifiedStaff()
    {
        // Act
        var service = NewDraftService();

        // Assert — a freshly created service is never bookable on its own
        Assert.Equal(ServiceStatus.Draft, service.Status);
        Assert.Empty(service.QualifiedStaff);
        Assert.Null(service.ActivatedAt);
    }

    [Fact]
    public void Activate_WithoutQualifiedStaff_Throws()
    {
        // Arrange — an organization's service nobody is qualified to perform
        var service = NewDraftService();

        // Act & Assert
        var ex = Assert.Throws<InvalidServiceException>(() => service.Activate());
        Assert.Contains("qualified staff member", ex.Message);
        Assert.Equal(ServiceStatus.Draft, service.Status);
    }

    [Fact]
    public void Activate_WhenProviderNavigationIsNotLoaded_StillRequiresQualifiedStaff()
    {
        // Arrange — inside a seeder/repository the Provider navigation is often null. An unknown hierarchy must be
        // treated as non-Individual, i.e. the safe side of the rule, never as a licence to activate.
        var service = NewDraftService();
        Assert.Null(service.Provider);

        // Act & Assert
        Assert.Throws<InvalidServiceException>(() => service.Activate());
    }

    [Fact]
    public void Activate_AfterAddingQualifiedStaff_Activates()
    {
        // Arrange
        var service = NewDraftService();
        var membershipId = Guid.NewGuid();
        service.AddQualifiedStaff(membershipId);

        // Act
        service.Activate();

        // Assert
        Assert.Equal(ServiceStatus.Active, service.Status);
        Assert.NotNull(service.ActivatedAt);
        Assert.True(service.IsStaffQualified(membershipId));
    }

    [Fact]
    public void Activate_ForAnIndividualProvider_NeedsNoQualifiedStaff()
    {
        // Arrange — a solo practitioner performs their own services
        var service = NewDraftService();
        AttachProvider(service, CreateProvider(ProviderHierarchyType.Individual));

        // Act
        service.Activate();

        // Assert
        Assert.Equal(ServiceStatus.Active, service.Status);
        Assert.Empty(service.QualifiedStaff);
    }

    [Fact]
    public void Activate_ForAnOrganizationProvider_StillRequiresQualifiedStaff()
    {
        // Arrange
        var service = NewDraftService();
        AttachProvider(service, CreateProvider(ProviderHierarchyType.Organization));

        // Act & Assert
        Assert.Throws<InvalidServiceException>(() => service.Activate());
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        // Arrange
        var service = NewDraftService();
        service.AddQualifiedStaff(Guid.NewGuid());
        service.Activate();

        // Act & Assert — callers must check the status first (the activation seeder does)
        Assert.Throws<InvalidServiceException>(() => service.Activate());
    }

    [Fact]
    public void Activate_RaisesServiceActivatedEvent()
    {
        // Arrange
        var providerId = ProviderId.New();
        var service = NewDraftService(providerId);
        service.AddQualifiedStaff(Guid.NewGuid());

        // Act
        service.Activate();

        // Assert
        var activated = Assert.Single(service.DomainEvents.OfType<ServiceActivatedEvent>());
        Assert.Equal(service.Id, activated.ServiceId);
        Assert.Equal(providerId, activated.ProviderId);
        Assert.Equal(service.ActivatedAt, activated.ActivatedAt);
    }

    #endregion

    #region Qualified-staff bookkeeping

    [Fact]
    public void AddQualifiedStaff_IsIdempotent()
    {
        // Arrange — the activation seeder may re-run over an existing database
        var service = NewDraftService();
        var membershipId = Guid.NewGuid();

        // Act
        service.AddQualifiedStaff(membershipId);
        service.AddQualifiedStaff(membershipId);

        // Assert
        Assert.Single(service.QualifiedStaff);
    }

    [Fact]
    public void AddQualifiedStaff_KeepsEveryDistinctMember()
    {
        // Arrange — a salon's service is performed by several of its members
        var service = NewDraftService();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        // Act
        service.AddQualifiedStaff(first);
        service.AddQualifiedStaff(second);

        // Assert
        Assert.Equal(2, service.QualifiedStaff.Count);
        Assert.True(service.IsStaffQualified(first));
        Assert.True(service.IsStaffQualified(second));
    }

    [Fact]
    public void RemoveQualifiedStaff_DropsTheMember()
    {
        // Arrange
        var service = NewDraftService();
        var membershipId = Guid.NewGuid();
        service.AddQualifiedStaff(membershipId);

        // Act
        service.RemoveQualifiedStaff(membershipId);

        // Assert
        Assert.Empty(service.QualifiedStaff);
        Assert.False(service.IsStaffQualified(membershipId));
    }

    #endregion

    #region CanBeBooked

    [Fact]
    public void CanBeBooked_ForAnOrganization_RequiresActiveStatusAndQualifiedStaff()
    {
        // Arrange
        var service = NewDraftService();
        AttachProvider(service, CreateProvider(ProviderHierarchyType.Organization));

        // Assert — draft, no staff
        Assert.False(service.CanBeBooked());

        // Act — qualify and activate, exactly as the seeder does
        service.AddQualifiedStaff(Guid.NewGuid());
        service.Activate();

        // Assert
        Assert.True(service.CanBeBooked());
    }

    [Fact]
    public void CanBeBooked_ForAnOrganizationWhoseLastMemberLeft_IsFalse()
    {
        // Arrange — an active service can outlive its staff; it must stop being bookable
        var service = NewDraftService();
        AttachProvider(service, CreateProvider(ProviderHierarchyType.Organization));
        var membershipId = Guid.NewGuid();
        service.AddQualifiedStaff(membershipId);
        service.Activate();

        // Act
        service.RemoveQualifiedStaff(membershipId);

        // Assert
        Assert.Equal(ServiceStatus.Active, service.Status);
        Assert.False(service.CanBeBooked());
    }

    [Fact]
    public void CanBeBooked_ForAnIndividual_NeedsOnlyActiveStatus()
    {
        // Arrange
        var service = NewDraftService();
        AttachProvider(service, CreateProvider(ProviderHierarchyType.Individual));

        // Assert
        Assert.False(service.CanBeBooked());

        // Act
        service.Activate();

        // Assert
        Assert.True(service.CanBeBooked());
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Builds a real Provider aggregate so the hierarchy branch of the rule is exercised against production
    /// behaviour rather than a stand-in.
    /// </summary>
    private static Provider CreateProvider(ProviderHierarchyType hierarchyType) =>
        Provider.CreateDraft(
            UserId.CreateNew(),
            "آرش",
            "مرادی",
            "آرایشگاه مردانه آریا",
            "پیرایش مردانه",
            ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create("arya.barber@booksy.ir"),
                PhoneNumber.From("09141110012")),
            BusinessAddress.Create(
                "خیابان سعدی، پارس آباد، اردبیل، ایران",
                "خیابان سعدی",
                "پارس آباد",
                "اردبیل",
                "5691",
                "ایران",
                null,
                null,
                39.6624,
                47.9302),
            hierarchyType);

    /// <summary>
    /// Sets the Provider navigation the way EF Core does. Service.Create only takes a ProviderId and the
    /// navigation has a private setter, so this is the only way to cover the hierarchy-dependent branch.
    /// </summary>
    private static void AttachProvider(Service service, Provider provider)
    {
        typeof(Service)
            .GetProperty(nameof(Service.Provider), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(service, provider);
    }

    #endregion
}
