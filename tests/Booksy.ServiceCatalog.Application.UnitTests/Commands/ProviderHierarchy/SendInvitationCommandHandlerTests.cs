using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderHierarchy;

public class SendInvitationCommandHandlerTests
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendInvitationCommandHandler> _logger;
    private readonly SendInvitationCommandHandler _handler;

    public SendInvitationCommandHandlerTests()
    {
        _providerRepository = Substitute.For<IProviderReadRepository>();
        _invitationReadRepository = Substitute.For<IProviderInvitationReadRepository>();
        _invitationWriteRepository = Substitute.For<IProviderInvitationWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<SendInvitationCommandHandler>>();

        _handler = new SendInvitationCommandHandler(
            _providerRepository,
            _invitationReadRepository,
            _invitationWriteRepository,
            _unitOfWork,
            _logger);
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

    [Fact]
    public async Task Handle_Should_Create_Invitation_Successfully()
    {
        // Arrange
        var organization = CreateOrganization();
        var command = new SendInvitationCommand(
            OrganizationId: organization.Id.Value,
            PhoneNumber: "+989121234567",
            InviteeName: "John Doe",
            Message: "Welcome!");

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        _invitationReadRepository.GetByPhoneNumberAndOrganizationAsync(
            Arg.Any<string>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((ProviderInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InvitationId.Should().NotBeEmpty();
        result.OrganizationId.Should().Be(organization.Id.Value);
        result.PhoneNumber.Should().Be("+989121234567");
        result.Status.Should().Be("Pending");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await _invitationWriteRepository.Received(1).SaveAsync(
            Arg.Is<ProviderInvitation>(i =>
                i.OrganizationId == organization.Id &&
                i.InviteeName == "John Doe"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Organization_Not_Found()
    {
        // Arrange
        var command = new SendInvitationCommand(
            OrganizationId: Guid.NewGuid(),
            PhoneNumber: "+989121234567");

        _providerRepository.GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((Provider?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Provider_Is_Not_Organization()
    {
        // Arrange
        var individual = CreateIndividualProvider();
        var command = new SendInvitationCommand(
            OrganizationId: individual.Id.Value,
            PhoneNumber: "+989121234567");

        _providerRepository.GetByIdAsync(individual.Id, Arg.Any<CancellationToken>())
            .Returns(individual);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*Only organizations can send invitations*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Pending_Invitation_Exists()
    {
        // Arrange
        var organization = CreateOrganization();
        var command = new SendInvitationCommand(
            OrganizationId: organization.Id.Value,
            PhoneNumber: "+989121234567");

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        var existingInvitation = ProviderInvitation.Create(
            organization.Id,
            PhoneNumber.Create("+989121234567"));

        _invitationReadRepository.GetByPhoneNumberAndOrganizationAsync(
            Arg.Any<string>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns(existingInvitation);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*pending invitation already exists*");
    }

    [Fact]
    public async Task Handle_Should_Create_Invitation_Without_Optional_Fields()
    {
        // Arrange
        var organization = CreateOrganization();
        var command = new SendInvitationCommand(
            OrganizationId: organization.Id.Value,
            PhoneNumber: "+989121234567");

        _providerRepository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns(organization);

        _invitationReadRepository.GetByPhoneNumberAndOrganizationAsync(
            Arg.Any<string>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((ProviderInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InvitationId.Should().NotBeEmpty();

        await _invitationWriteRepository.Received(1).SaveAsync(
            Arg.Is<ProviderInvitation>(i =>
                i.InviteeName == null &&
                i.Message == null),
            Arg.Any<CancellationToken>());
    }
}
