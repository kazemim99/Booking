using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderHierarchy;

public class ApproveJoinRequestCommandHandlerTests
{
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderJoinRequestReadRepository _joinRequestReadRepository;
    private readonly IProviderJoinRequestWriteRepository _joinRequestWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveJoinRequestCommandHandler> _logger;
    private readonly ApproveJoinRequestCommandHandler _handler;

    public ApproveJoinRequestCommandHandlerTests()
    {
        _providerReadRepository = Substitute.For<IProviderReadRepository>();
        _providerWriteRepository = Substitute.For<IProviderWriteRepository>();
        _joinRequestReadRepository = Substitute.For<IProviderJoinRequestReadRepository>();
        _joinRequestWriteRepository = Substitute.For<IProviderJoinRequestWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<ApproveJoinRequestCommandHandler>>();

        _handler = new ApproveJoinRequestCommandHandler(
            _providerReadRepository,
            _providerWriteRepository,
            _joinRequestReadRepository,
            _joinRequestWriteRepository,
            _unitOfWork,
            _logger);
    }

    private static Provider CreateOrganization()
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Test Salon",
            "Test description",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("salon@test.com"), PhoneNumber.From("+989123456789")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Organization);
    }

    private static Provider CreateIndividualProvider()
    {
        return Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Solo Barber",
            "Test description",
            ServiceCategory.Barbershop,
            ContactInfo.Create(Email.Create("barber@test.com"), PhoneNumber.From("+989123456788")),
            BusinessAddress.Create("123 Test St", "Suite 1", "Test City", "TS", "12345", "IR"),
            ProviderHierarchyType.Individual);
    }

    [Fact]
    public async Task Handle_Should_Approve_Join_Request_Successfully()
    {
        // Arrange
        var organization = CreateOrganization();
        var requester = CreateIndividualProvider();
        var joinRequest = ProviderJoinRequest.Create(organization.Id, requester.Id, "I want to join");
        var reviewerId = Guid.NewGuid();

        var command = new ApproveJoinRequestCommand(
            RequestId: joinRequest.Id,
            ReviewerId: reviewerId,
            Note: "Welcome aboard!");

        _joinRequestReadRepository.GetByIdAsync(joinRequest.Id, Arg.Any<CancellationToken>())
            .Returns(joinRequest);

        _providerReadRepository.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(requester);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be(joinRequest.Id);
        result.OrganizationId.Should().Be(organization.Id.Value);
        result.RequesterId.Should().Be(requester.Id.Value);
        result.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        joinRequest.Status.Should().Be(JoinRequestStatus.Approved);
        joinRequest.ReviewedBy.Should().Be(reviewerId);
        joinRequest.ReviewNote.Should().Be("Welcome aboard!");

        requester.ParentProviderId.Should().Be(organization.Id);
        requester.IsIndependent.Should().BeFalse();

        await _joinRequestWriteRepository.Received(1).UpdateAsync(joinRequest, Arg.Any<CancellationToken>());
        await _providerWriteRepository.Received(1).UpdateAsync(requester, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Join_Request_Not_Found()
    {
        // Arrange
        var command = new ApproveJoinRequestCommand(
            RequestId: Guid.NewGuid(),
            ReviewerId: Guid.NewGuid());

        _joinRequestReadRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProviderJoinRequest?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Request_Not_Pending()
    {
        // Arrange
        var organization = CreateOrganization();
        var requester = CreateIndividualProvider();
        var joinRequest = ProviderJoinRequest.Create(organization.Id, requester.Id);
        joinRequest.Reject(Guid.NewGuid()); // Already rejected

        var command = new ApproveJoinRequestCommand(
            RequestId: joinRequest.Id,
            ReviewerId: Guid.NewGuid());

        _joinRequestReadRepository.GetByIdAsync(joinRequest.Id, Arg.Any<CancellationToken>())
            .Returns(joinRequest);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*no longer pending*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Requester_Not_Found()
    {
        // Arrange
        var organization = CreateOrganization();
        var requesterId = ProviderId.New();
        var joinRequest = ProviderJoinRequest.Create(organization.Id, requesterId);

        var command = new ApproveJoinRequestCommand(
            RequestId: joinRequest.Id,
            ReviewerId: Guid.NewGuid());

        _joinRequestReadRepository.GetByIdAsync(joinRequest.Id, Arg.Any<CancellationToken>())
            .Returns(joinRequest);

        _providerReadRepository.GetByIdAsync(requesterId, Arg.Any<CancellationToken>())
            .Returns((Provider?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Requester provider*not found*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Requester_Already_Linked()
    {
        // Arrange
        var organization = CreateOrganization();
        var anotherOrganization = CreateOrganization();
        var requester = CreateIndividualProvider();
        requester.LinkToOrganization(anotherOrganization.Id); // Already linked

        var joinRequest = ProviderJoinRequest.Create(organization.Id, requester.Id);

        var command = new ApproveJoinRequestCommand(
            RequestId: joinRequest.Id,
            ReviewerId: Guid.NewGuid());

        _joinRequestReadRepository.GetByIdAsync(joinRequest.Id, Arg.Any<CancellationToken>())
            .Returns(joinRequest);

        _providerReadRepository.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(requester);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*already linked*");
    }

    [Fact]
    public async Task Handle_Should_Work_Without_Optional_Note()
    {
        // Arrange
        var organization = CreateOrganization();
        var requester = CreateIndividualProvider();
        var joinRequest = ProviderJoinRequest.Create(organization.Id, requester.Id);

        var command = new ApproveJoinRequestCommand(
            RequestId: joinRequest.Id,
            ReviewerId: Guid.NewGuid());

        _joinRequestReadRepository.GetByIdAsync(joinRequest.Id, Arg.Any<CancellationToken>())
            .Returns(joinRequest);

        _providerReadRepository.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(requester);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        joinRequest.ReviewNote.Should().BeNull();
    }
}
