using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetProviderWithStaff;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Queries.ProviderHierarchy;

public class GetProviderWithStaffQueryHandlerTests
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly ILogger<GetProviderWithStaffQueryHandler> _logger;
    private readonly GetProviderWithStaffQueryHandler _handler;

    public GetProviderWithStaffQueryHandlerTests()
    {
        _providerRepository = Substitute.For<IProviderReadRepository>();
        _logger = Substitute.For<ILogger<GetProviderWithStaffQueryHandler>>();

        _handler = new GetProviderWithStaffQueryHandler(_providerRepository, _logger);
    }

    private static Provider CreateOrganization(string name = "Test Salon")
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            name,
            "Test description",
            ProviderType.Individual,
            ContactInfo.Create(Email.Create("salon@test.com"), PhoneNumber.Create("+989123456789")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Organization);
    }

    private static Provider CreateIndividualProvider(string name = "Solo Barber")
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            name,
            "Test description",
            ProviderType.Individual,
            ContactInfo.Create(Email.Create("barber@test.com"), PhoneNumber.Create("+989123456788")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Individual);
    }

    [Fact]
    public async Task Handle_Should_Return_Organization_With_Staff()
    {
        // Arrange
        var organization = CreateOrganization();
        var staffMember1 = CreateIndividualProvider("Stylist 1");
        var staffMember2 = CreateIndividualProvider("Stylist 2");

        staffMember1.LinkToOrganization(organization.Id);
        staffMember2.LinkToOrganization(organization.Id);

        var query = new GetProviderWithStaffQuery(organization.Id.Value);

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        _providerRepository.GetStaffByOrganizationIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Provider> { staffMember1, staffMember2 });

        _providerRepository.CountStaffByOrganizationAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(organization.Id.Value);
        result.BusinessName.Should().Be("Test Salon");
        result.HierarchyType.Should().Be("Organization");
        result.IsIndependent.Should().BeFalse();
        result.ParentProviderId.Should().BeNull();
        result.ParentOrganization.Should().BeNull();
        result.StaffMembers.Should().HaveCount(2);
        result.TotalStaffCount.Should().Be(2);

        result.StaffMembers[0].BusinessName.Should().Be("Stylist 1");
        result.StaffMembers[1].BusinessName.Should().Be("Stylist 2");
    }

    [Fact]
    public async Task Handle_Should_Return_Individual_With_Parent_Organization()
    {
        // Arrange
        var organization = CreateOrganization();
        var individual = CreateIndividualProvider();
        individual.LinkToOrganization(organization.Id);

        var query = new GetProviderWithStaffQuery(individual.Id.Value);

        _providerRepository.GetByIdAsync(individual.Id, Arg.Any<CancellationToken>())
            .Returns(individual);

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(individual.Id.Value);
        result.HierarchyType.Should().Be("Individual");
        result.IsIndependent.Should().BeFalse();
        result.ParentProviderId.Should().Be(organization.Id.Value);
        result.ParentOrganization.Should().NotBeNull();
        result.ParentOrganization!.ProviderId.Should().Be(organization.Id.Value);
        result.ParentOrganization.BusinessName.Should().Be("Test Salon");
        result.StaffMembers.Should().BeEmpty();
        result.TotalStaffCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Return_Independent_Individual()
    {
        // Arrange
        var individual = CreateIndividualProvider();

        var query = new GetProviderWithStaffQuery(individual.Id.Value);

        _providerRepository.GetByIdAsync(individual.Id, Arg.Any<CancellationToken>())
            .Returns(individual);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(individual.Id.Value);
        result.HierarchyType.Should().Be("Individual");
        result.IsIndependent.Should().BeTrue();
        result.ParentProviderId.Should().BeNull();
        result.ParentOrganization.Should().BeNull();
        result.StaffMembers.Should().BeEmpty();
        result.TotalStaffCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Provider_Not_Found()
    {
        // Arrange
        var query = new GetProviderWithStaffQuery(Guid.NewGuid());

        _providerRepository.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((Provider?)null);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Should_Return_Organization_Without_Staff()
    {
        // Arrange
        var organization = CreateOrganization();

        var query = new GetProviderWithStaffQuery(organization.Id.Value);

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        _providerRepository.GetStaffByOrganizationIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Provider>());

        _providerRepository.CountStaffByOrganizationAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HierarchyType.Should().Be("Organization");
        result.StaffMembers.Should().BeEmpty();
        result.TotalStaffCount.Should().Be(0);
    }
}
