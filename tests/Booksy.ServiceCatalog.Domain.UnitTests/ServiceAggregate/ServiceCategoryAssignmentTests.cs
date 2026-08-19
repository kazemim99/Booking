using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ServiceAggregate;

/// <summary>
/// Unit tests for <c>Service.Category</c> after its move from a value object to the
/// ServiceCategory enum.
///
/// <para>The column is now an int, so an undefined value is silently storable — these
/// tests pin the aggregate as the gate that keeps <c>services.Category</c> holding only
/// values the display metadata can render.</para>
///
/// <para>Note: cross-checking a service's category against its provider's
/// <c>PrimaryCategory</c> is deliberately out of scope (design.md, Decision 7 defers the
/// compatibility matrix), so a spa offering a haircut is accepted here on purpose.</para>
/// </summary>
public class ServiceCategoryAssignmentTests
{
    private static Service NewService(ServiceCategory category) =>
        Service.Create(
            ProviderId.New(),
            "Test Service",
            "Description",
            category,
            ServiceType.Standard,
            Price.Create(500_000m, "IRR"),
            Duration.FromMinutes(30));

    #region Category is captured at creation

    [Theory]
    [InlineData(ServiceCategory.HairSalon)]
    [InlineData(ServiceCategory.Massage)]
    [InlineData(ServiceCategory.Dental)]
    [InlineData(ServiceCategory.HomeServices)]
    public void Create_StoresTheChosenCategory(ServiceCategory category)
    {
        var service = NewService(category);

        Assert.Equal(category, service.Category);
    }

    [Fact]
    public void Create_AnnouncesTheCategoryOnTheCreatedEvent()
    {
        var service = NewService(ServiceCategory.Yoga);

        var created = Assert.Single(service.DomainEvents.OfType<ServiceCreatedEvent>());
        Assert.Equal(ServiceCategory.Yoga, created.Category);
    }

    #endregion

    #region Undefined categories are refused

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Create_RejectsAnUndefinedCategory(int rawCategory)
    {
        var ex = Assert.Throws<InvalidServiceException>(() => NewService((ServiceCategory)rawCategory));

        Assert.Contains("not a valid service category", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void UpdateBasicInfo_RejectsAnUndefinedCategory(int rawCategory)
    {
        var service = NewService(ServiceCategory.HairSalon);

        Assert.Throws<InvalidServiceException>(() =>
            service.UpdateBasicInfo("New name", "New description", (ServiceCategory)rawCategory));

        Assert.Equal(ServiceCategory.HairSalon, service.Category);
        Assert.Equal("Test Service", service.Name);
    }

    #endregion

    #region Recategorising a service

    [Fact]
    public void UpdateBasicInfo_MovesTheServiceToTheNewCategory()
    {
        var service = NewService(ServiceCategory.HairSalon);

        service.UpdateBasicInfo("Deep Tissue Massage", "60 minute massage", ServiceCategory.Massage);

        Assert.Equal(ServiceCategory.Massage, service.Category);
        Assert.Equal("Deep Tissue Massage", service.Name);
    }

    [Fact]
    public void ServiceCategory_NeedNotMatchTheProvidersPrimaryCategory()
    {
        // Decision 7: alignment validation is deferred, so a spa can list a haircut today.
        // If a compatibility matrix ever lands, this test is the one to rewrite.
        var providerId = ProviderId.New();

        var service = Service.Create(
            providerId,
            "Haircut at the spa",
            "Cross-category offering",
            ServiceCategory.HairSalon,
            ServiceType.Standard,
            Price.Create(500_000m, "IRR"),
            Duration.FromMinutes(30));

        Assert.Equal(ServiceCategory.HairSalon, service.Category);
    }

    #endregion
}
