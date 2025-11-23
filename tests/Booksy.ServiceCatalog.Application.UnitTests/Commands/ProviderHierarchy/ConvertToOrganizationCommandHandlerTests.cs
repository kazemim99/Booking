using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderHierarchy;

public class ConvertToOrganizationCommandHandlerTests
{
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConvertToOrganizationCommandHandler> _logger;
    private readonly ConvertToOrganizationCommandHandler _handler;

    public ConvertToOrganizationCommandHandlerTests()
    {
        _providerReadRepository = Substitute.For<IProviderReadRepository>();
        _providerWriteRepository = Substitute.For<IProviderWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<ConvertToOrganizationCommandHandler>>();

        _handler = new ConvertToOrganizationCommandHandler(
            _providerReadRepository,
            _providerWriteRepository,
            _unitOfWork,
            _logger);
    }

    private static Provider CreateIndividualProvider()
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Solo Barber",
            "Test description",
            ProviderType.Individual,
            ContactInfo.Create(Email.Create("barber@test.com"), PhoneNumber.Create("+989123456788")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Individual);
    }

    private static Provider CreateOrganization()
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Test Salon",
            "Test description",
            ProviderType.Individual,
            ContactInfo.Create(Email.Create("salon@test.com"), PhoneNumber.Create("+989123456789")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Organization);
    }

    [Fact]
    public async Task Handle_Should_Convert_Individual_To_Organization()
    {
        // Arrange
        var individual = CreateIndividualProvider();
        var command = new ConvertToOrganizationCommand(individual.Id.Value);

        _providerReadRepository.GetByIdAsync(individual.Id, Arg.Any<CancellationToken>())
            .Returns(individual);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(individual.Id.Value);
        result.HierarchyType.Should().Be("Organization");
        result.ConvertedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        individual.HierarchyType.Should().Be(ProviderHierarchyType.Organization);
        individual.IsIndependent.Should().BeFalse();
        individual.CanHaveStaff().Should().BeTrue();

        await _providerWriteRepository.Received(1).UpdateAsync(individual, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Provider_Not_Found()
    {
        // Arrange
        var command = new ConvertToOrganizationCommand(Guid.NewGuid());

        _providerReadRepository.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((Provider?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Already_Organization()
    {
        // Arrange
        var organization = CreateOrganization();
        var command = new ConvertToOrganizationCommand(organization.Id.Value);

        _providerReadRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*already an organization*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Linked_To_Organization()
    {
        // Arrange
        var parentOrganization = CreateOrganization();
        var individual = CreateIndividualProvider();
        individual.LinkToOrganization(parentOrganization.Id);

        var command = new ConvertToOrganizationCommand(individual.Id.Value);

        _providerReadRepository.GetByIdAsync(individual.Id, Arg.Any<CancellationToken>())
            .Returns(individual);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*staff member*leave parent organization*");
    }
}
