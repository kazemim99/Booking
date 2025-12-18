using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ProviderAggregate;

/// <summary>
/// Unit tests for Provider hierarchy functionality
/// Tests the domain logic for organizations, individuals, and parent-child relationships
/// </summary>
public class ProviderHierarchyTests
{
    private readonly UserId _ownerId = UserId.From(Guid.NewGuid());
    private readonly ContactInfo _contactInfo = ContactInfo.Create(
        Email.Create("test@example.com"),
        PhoneNumber.From("+1234567890"));
    private readonly BusinessAddress _address = BusinessAddress.Create(
        "123 Test St",
        "Suite 100",
        "Test City",
        "TS",
        "12345",
        "USA");

    #region Provider Creation with Hierarchy Type

    [Fact]
    public void RegisterProvider_Should_Default_To_Organization_Type()
    {
        // Arrange & Act
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Test Salon",
            "A great salon",
            ProviderType.Individual,
            _contactInfo,
            _address);

        // Assert
        Assert.Equal(ProviderHierarchyType.Organization, provider.HierarchyType);
        Assert.False(provider.IsIndependent);
        Assert.Null(provider.ParentProviderId);
    }

    [Fact]
    public void RegisterProvider_Should_Create_Organization_When_Specified()
    {
        // Arrange & Act
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        // Assert
        Assert.Equal(ProviderHierarchyType.Organization, provider.HierarchyType);
        Assert.False(provider.IsIndependent);
        Assert.Null(provider.ParentProviderId);
        Assert.True(provider.CanHaveStaff());
    }

    [Fact]
    public void RegisterProvider_Should_Create_Independent_Individual_When_Specified()
    {
        // Arrange & Act
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Mobile Barber",
            "Professional barber services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Assert
        Assert.Equal(ProviderHierarchyType.Individual, provider.HierarchyType);
        Assert.True(provider.IsIndependent);
        Assert.Null(provider.ParentProviderId);
        Assert.False(provider.CanHaveStaff());
    }

    [Fact]
    public void CreateDraft_Should_Create_Organization_By_Default()
    {
        // Arrange & Act
        var provider = Provider.CreateDraft(
            _ownerId,
            "John",
            "Doe",
            "Test Business",
            "Test description",
            ProviderType.Individual,
            _contactInfo,
            _address);

        // Assert
        Assert.Equal(ProviderHierarchyType.Organization, provider.HierarchyType);
        Assert.False(provider.IsIndependent);
    }

    [Fact]
    public void CreateDraft_Should_Create_Individual_When_Specified()
    {
        // Arrange & Act
        var provider = Provider.CreateDraft(
            _ownerId,
            "Jane",
            "Smith",
            "Freelance Stylist",
            "Expert styling",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Assert
        Assert.Equal(ProviderHierarchyType.Individual, provider.HierarchyType);
        Assert.True(provider.IsIndependent);
    }

    #endregion

    #region ConvertToOrganization Tests

    [Fact]
    public void ConvertToOrganization_Should_Change_Individual_To_Organization()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Solo Barber",
            "Expert cuts",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act
        provider.ConvertToOrganization();

        // Assert
        Assert.Equal(ProviderHierarchyType.Organization, provider.HierarchyType);
        Assert.False(provider.IsIndependent);
        Assert.True(provider.CanHaveStaff());
    }

    [Fact]
    public void ConvertToOrganization_Should_Raise_Domain_Event()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Solo Barber",
            "Expert cuts",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act
        provider.ConvertToOrganization();

        // Assert
        var domainEvents = provider.DomainEvents;
        Assert.Contains(domainEvents, e => e is ProviderConvertedToOrganizationEvent);
    }

    [Fact]
    public void ConvertToOrganization_Should_Throw_When_Already_Organization()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.ConvertToOrganization());
        Assert.Contains("already an organization", exception.Message);
    }

    [Fact]
    public void ConvertToOrganization_Should_Throw_When_Linked_To_Organization()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Staff Member",
            "Stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.ConvertToOrganization());
        Assert.Contains("leave parent organization", exception.Message);
    }

    #endregion

    #region LinkToOrganization Tests

    [Fact]
    public void LinkToOrganization_Should_Set_Parent_Provider_Id()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act
        provider.LinkToOrganization(organizationId);

        // Assert
        Assert.Equal(organizationId, provider.ParentProviderId);
        Assert.False(provider.IsIndependent);
    }

    [Fact]
    public void LinkToOrganization_Should_Raise_Domain_Event()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act
        provider.LinkToOrganization(organizationId);

        // Assert
        var domainEvents = provider.DomainEvents;
        Assert.Contains(domainEvents, e => e is StaffMemberAddedToOrganizationEvent);
    }

    [Fact]
    public void LinkToOrganization_Should_Throw_When_Provider_Is_Organization()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Salon",
            "Great salon",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.LinkToOrganization(organizationId));
        Assert.Contains("Only individual providers", exception.Message);
    }

    [Fact]
    public void LinkToOrganization_Should_Throw_When_Already_Linked()
    {
        // Arrange
        var organizationId1 = ProviderId.New();
        var organizationId2 = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId1);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.LinkToOrganization(organizationId2));
        Assert.Contains("already linked", exception.Message);
    }

    [Fact]
    public void LinkToOrganization_Should_Throw_When_Linking_To_Self()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.LinkToOrganization(provider.Id));
        Assert.Contains("cannot be its own parent", exception.Message);
    }

    #endregion

    #region UnlinkFromOrganization Tests

    [Fact]
    public void UnlinkFromOrganization_Should_Clear_Parent_And_Set_Independent()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId);

        // Act
        provider.UnlinkFromOrganization("Moving to different salon");

        // Assert
        Assert.Null(provider.ParentProviderId);
        Assert.True(provider.IsIndependent);
    }

    [Fact]
    public void UnlinkFromOrganization_Should_Raise_Domain_Event()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId);

        // Act
        provider.UnlinkFromOrganization("Leaving");

        // Assert
        var domainEvents = provider.DomainEvents;
        Assert.Contains(domainEvents, e => e is StaffMemberRemovedFromOrganizationEvent);
    }

    [Fact]
    public void UnlinkFromOrganization_Should_Throw_When_Not_Linked()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Independent Stylist",
            "Solo professional",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act & Assert
        var exception = Assert.Throws<InvalidProviderException>(() => provider.UnlinkFromOrganization("Reason"));
        Assert.Contains("not linked", exception.Message);
    }

    #endregion

    #region CanAcceptDirectBookings Tests

    [Fact]
    public void CanAcceptDirectBookings_Should_Return_True_For_Active_Organization()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        provider.SetSatus(ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        // Act
        var canAccept = provider.CanAcceptDirectBookings();

        // Assert
        Assert.True(canAccept);
    }

    [Fact]
    public void CanAcceptDirectBookings_Should_Return_True_For_Independent_Individual()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Mobile Barber",
            "Expert cuts",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.SetSatus(ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        // Act
        var canAccept = provider.CanAcceptDirectBookings();

        // Assert
        Assert.True(canAccept);
    }

    [Fact]
    public void CanAcceptDirectBookings_Should_Return_True_For_Linked_Staff_Member()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId);
        provider.SetSatus(ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        // Act
        var canAccept = provider.CanAcceptDirectBookings();

        // Assert
        Assert.True(canAccept);
    }

    [Fact]
    public void CanAcceptDirectBookings_Should_Return_False_When_Not_Active()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Test Salon",
            "Great services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        provider.SetAllowOnlineBooking(true);
        // Status is PendingVerification by default

        // Act
        var canAccept = provider.CanAcceptDirectBookings();

        // Assert
        Assert.False(canAccept);
    }

    [Fact]
    public void CanAcceptDirectBookings_Should_Return_False_When_Online_Booking_Disabled()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Test Salon",
            "Great services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        provider.SetSatus(ProviderStatus.Active);
        provider.SetAllowOnlineBooking(false);

        // Act
        var canAccept = provider.CanAcceptDirectBookings();

        // Assert
        Assert.False(canAccept);
    }

    #endregion

    #region CanHaveStaff Tests

    [Fact]
    public void CanHaveStaff_Should_Return_True_For_Organization()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        // Act & Assert
        Assert.True(provider.CanHaveStaff());
    }

    [Fact]
    public void CanHaveStaff_Should_Return_False_For_Individual()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Solo Barber",
            "Expert cuts",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act & Assert
        Assert.False(provider.CanHaveStaff());
    }

    #endregion

    #region ValidateHierarchy Tests

    [Fact]
    public void ValidateHierarchy_Should_Pass_For_Valid_Organization()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Organization);

        // Act & Assert - should not throw
        var exception = Record.Exception(() => provider.ValidateHierarchy());
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateHierarchy_Should_Pass_For_Valid_Independent_Individual()
    {
        // Arrange
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Solo Barber",
            "Expert cuts",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        // Act & Assert - should not throw
        var exception = Record.Exception(() => provider.ValidateHierarchy());
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateHierarchy_Should_Pass_For_Valid_Linked_Individual()
    {
        // Arrange
        var organizationId = ProviderId.New();
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Stylist",
            "Expert stylist",
            ProviderType.Individual,
            _contactInfo,
            _address,
            ProviderHierarchyType.Individual);

        provider.LinkToOrganization(organizationId);

        // Act & Assert - should not throw
        var exception = Record.Exception(() => provider.ValidateHierarchy());
        Assert.Null(exception);
    }

    #endregion
}
