using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ProviderAggregate;

/// <summary>
/// Unit tests for the "every provider has exactly one valid primary category" rule.
///
/// <para>Categories reach the aggregate as strings from registration payloads, and
/// <c>Enum.TryParse&lt;ServiceCategory&gt;</c> accepts any number — "0" and "99" parse
/// happily into values no display metadata exists for. These tests pin the aggregate as
/// the place that refuses those values, so an unrenderable provider can never be
/// persisted regardless of which handler is doing the parsing.</para>
/// </summary>
public class ProviderPrimaryCategoryTests
{
    private readonly UserId _ownerId = UserId.From(Guid.NewGuid());

    private static ContactInfo NewContactInfo() => ContactInfo.Create(
        Email.Create("owner@salon.example"),
        PhoneNumber.From("+989120000000"));

    private static BusinessAddress NewAddress() => BusinessAddress.Create(
        "خیابان ولیعصر، پلاک ۱۲",
        "خیابان ولیعصر",
        "تهران",
        "تهران",
        "1234567890",
        "IR");

    #region Category is captured at creation

    [Theory]
    [InlineData(ServiceCategory.HairSalon)]
    [InlineData(ServiceCategory.Barbershop)]
    [InlineData(ServiceCategory.MedicalClinic)]
    [InlineData(ServiceCategory.PetCare)]
    public void RegisterProvider_StoresTheChosenCategory(ServiceCategory category)
    {
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Test Business",
            "Description",
            category,
            NewContactInfo(),
            NewAddress());

        Assert.Equal(category, provider.PrimaryCategory);
    }

    [Theory]
    [InlineData(ServiceCategory.BeautySalon)]
    [InlineData(ServiceCategory.Gym)]
    public void CreateDraft_StoresTheChosenCategory(ServiceCategory category)
    {
        var provider = Provider.CreateDraft(
            _ownerId,
            "Sara",
            "Ahmadi",
            "Test Business",
            "Description",
            category,
            NewContactInfo(),
            NewAddress());

        Assert.Equal(category, provider.PrimaryCategory);
        Assert.Equal(ProviderStatus.Drafted, provider.Status);
    }

    [Fact]
    public void RegisterProvider_AnnouncesTheCategoryOnTheRegisteredEvent()
    {
        // Downstream contexts key off this event, so the category has to travel with it.
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Spa",
            "Premium wellness",
            ServiceCategory.Spa,
            NewContactInfo(),
            NewAddress());

        var registered = Assert.Single(provider.DomainEvents.OfType<ProviderRegisteredEvent>());
        Assert.Equal(ServiceCategory.Spa, registered.PrimaryCategory);
    }

    [Fact]
    public void Category_IsIndependentOfHierarchyType()
    {
        // An individual freelancer is categorised exactly like an organization is.
        var individual = Provider.RegisterProvider(
            _ownerId,
            "Mobile Barber",
            "Home visits",
            ServiceCategory.Barbershop,
            NewContactInfo(),
            NewAddress(),
            ProviderHierarchyType.Individual);

        Assert.Equal(ProviderHierarchyType.Individual, individual.HierarchyType);
        Assert.Equal(ServiceCategory.Barbershop, individual.PrimaryCategory);
    }

    #endregion

    #region Undefined categories are refused

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(-1)]
    [InlineData(99)]
    public void RegisterProvider_RejectsAnUndefinedCategory(int rawCategory)
    {
        var ex = Assert.Throws<InvalidProviderException>(() => Provider.RegisterProvider(
            _ownerId,
            "Test Business",
            "Description",
            (ServiceCategory)rawCategory,
            NewContactInfo(),
            NewAddress()));

        Assert.Contains("not a valid service category", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void CreateDraft_RejectsAnUndefinedCategory(int rawCategory)
    {
        var ex = Assert.Throws<InvalidProviderException>(() => Provider.CreateDraft(
            _ownerId,
            "Sara",
            "Ahmadi",
            "Test Business",
            "Description",
            (ServiceCategory)rawCategory,
            NewContactInfo(),
            NewAddress()));

        Assert.Contains("not a valid service category", ex.Message);
    }

    #endregion

    #region Changing the category while still a draft

    [Fact]
    public void UpdateDraftInfo_CanCorrectTheCategoryBeforeRegistrationCompletes()
    {
        // Going "back" in the registration wizard to fix a mis-picked category is allowed.
        var provider = Provider.CreateDraft(
            _ownerId,
            "Sara",
            "Ahmadi",
            "Sara's Studio",
            "Description",
            ServiceCategory.HairSalon,
            NewContactInfo(),
            NewAddress());

        provider.UpdateDraftInfo(
            "Sara",
            "Ahmadi",
            "Sara's Studio",
            "Description",
            ServiceCategory.NailSalon,
            NewContactInfo(),
            NewAddress());

        Assert.Equal(ServiceCategory.NailSalon, provider.PrimaryCategory);
    }

    [Fact]
    public void UpdateDraftInfo_RejectsAnUndefinedCategory()
    {
        var provider = Provider.CreateDraft(
            _ownerId,
            "Sara",
            "Ahmadi",
            "Sara's Studio",
            "Description",
            ServiceCategory.HairSalon,
            NewContactInfo(),
            NewAddress());

        Assert.Throws<InvalidProviderException>(() => provider.UpdateDraftInfo(
            "Sara",
            "Ahmadi",
            "Sara's Studio",
            "Description",
            (ServiceCategory)0,
            NewContactInfo(),
            NewAddress()));

        Assert.Equal(ServiceCategory.HairSalon, provider.PrimaryCategory);
    }

    [Fact]
    public void UpdateDraftInfo_IsRejectedOnceTheProviderIsNoLongerADraft()
    {
        // The category is settled at the end of registration; changing it later is an
        // admin-approval concern, not something the registration path may do.
        var provider = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Description",
            ServiceCategory.HairSalon,
            NewContactInfo(),
            NewAddress());

        Assert.Throws<InvalidOperationException>(() => provider.UpdateDraftInfo(
            "Sara",
            "Ahmadi",
            "Elite Salon",
            "Description",
            ServiceCategory.Spa,
            NewContactInfo(),
            NewAddress()));

        Assert.Equal(ServiceCategory.HairSalon, provider.PrimaryCategory);
    }

    #endregion

    #region Staff sub-providers

    [Fact]
    public void RegisterStaffMember_InheritsTheOrganizationsCategory()
    {
        // Staff are modelled as sub-providers; they must show up under the same category
        // as the organization they belong to.
        var organization = Provider.RegisterProvider(
            _ownerId,
            "Elite Salon",
            "Premium services",
            ServiceCategory.BeautySalon,
            NewContactInfo(),
            NewAddress(),
            ProviderHierarchyType.Organization);

        var staff = Provider.RegisterStaffMember(
            organization,
            UserId.From(Guid.NewGuid()),
            "Mina",
            "Rezaei");

        Assert.Equal(ServiceCategory.BeautySalon, staff.PrimaryCategory);
        Assert.Equal(organization.Id, staff.ParentProviderId);
    }

    #endregion
}
