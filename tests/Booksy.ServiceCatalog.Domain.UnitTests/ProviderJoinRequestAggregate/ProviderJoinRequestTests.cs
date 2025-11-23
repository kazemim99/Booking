using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ProviderJoinRequestAggregate;

/// <summary>
/// Unit tests for ProviderJoinRequest aggregate
/// Tests the join request workflow for individuals requesting to join organizations
/// </summary>
public class ProviderJoinRequestTests
{
    private readonly ProviderId _organizationId = ProviderId.New();
    private readonly ProviderId _requesterId = ProviderId.New();
    private readonly Guid _reviewerId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_Should_Create_JoinRequest_With_Pending_Status()
    {
        // Arrange & Act
        var request = ProviderJoinRequest.Create(
            _organizationId,
            _requesterId,
            "I would like to join your salon as a stylist");

        // Assert
        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(_organizationId, request.OrganizationId);
        Assert.Equal(_requesterId, request.RequesterId);
        Assert.Equal("I would like to join your salon as a stylist", request.Message);
        Assert.Equal(JoinRequestStatus.Pending, request.Status);
        Assert.True((DateTime.UtcNow - request.CreatedAt).TotalSeconds < 5);
        Assert.Null(request.ReviewedAt);
        Assert.Null(request.ReviewedBy);
        Assert.Null(request.ReviewNote);
    }

    [Fact]
    public void Create_Should_Work_Without_Message()
    {
        // Arrange & Act
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Assert
        Assert.Null(request.Message);
        Assert.Equal(JoinRequestStatus.Pending, request.Status);
    }

    #endregion

    #region Approve Tests

    [Fact]
    public void Approve_Should_Change_Status_To_Approved()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Approve(_reviewerId, "Welcome to the team!");

        // Assert
        Assert.Equal(JoinRequestStatus.Approved, request.Status);
        Assert.NotNull(request.ReviewedAt);
        Assert.Equal(_reviewerId, request.ReviewedBy);
        Assert.Equal("Welcome to the team!", request.ReviewNote);
        Assert.True((DateTime.UtcNow - request.ReviewedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Approve_Should_Raise_JoinRequestApproved_Event()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Approve(_reviewerId);

        // Assert
        var domainEvents = request.DomainEvents;
        Assert.Contains(domainEvents, e => e is JoinRequestApprovedEvent);

        var approvedEvent = domainEvents.OfType<JoinRequestApprovedEvent>().First();
        Assert.Equal(request.Id, approvedEvent.JoinRequestId);
        Assert.Equal(_organizationId, approvedEvent.OrganizationId);
        Assert.Equal(_requesterId, approvedEvent.RequesterId);
    }

    [Fact]
    public void Approve_Should_Work_Without_Note()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Approve(_reviewerId);

        // Assert
        Assert.Equal(JoinRequestStatus.Approved, request.Status);
        Assert.Null(request.ReviewNote);
    }

    [Fact]
    public void Approve_Should_Throw_When_Already_Approved()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Approve(_reviewerId);

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            request.Approve(Guid.NewGuid()));
        Assert.Contains("Approved", exception.Message);
    }

    [Fact]
    public void Approve_Should_Throw_When_Rejected()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Reject(_reviewerId);

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            request.Approve(Guid.NewGuid()));
        Assert.Contains("Rejected", exception.Message);
    }

    [Fact]
    public void Approve_Should_Throw_When_Withdrawn()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Withdraw();

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            request.Approve(Guid.NewGuid()));
        Assert.Contains("Withdrawn", exception.Message);
    }

    #endregion

    #region Reject Tests

    [Fact]
    public void Reject_Should_Change_Status_To_Rejected()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Reject(_reviewerId, "Position already filled");

        // Assert
        Assert.Equal(JoinRequestStatus.Rejected, request.Status);
        Assert.NotNull(request.ReviewedAt);
        Assert.Equal(_reviewerId, request.ReviewedBy);
        Assert.Equal("Position already filled", request.ReviewNote);
    }

    [Fact]
    public void Reject_Should_Work_Without_Note()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Reject(_reviewerId);

        // Assert
        Assert.Equal(JoinRequestStatus.Rejected, request.Status);
        Assert.Null(request.ReviewNote);
    }

    [Fact]
    public void Reject_Should_Throw_When_Not_Pending()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Approve(_reviewerId);

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => request.Reject(Guid.NewGuid()));
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public void Withdraw_Should_Change_Status_To_Withdrawn()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act
        request.Withdraw();

        // Assert
        Assert.Equal(JoinRequestStatus.Withdrawn, request.Status);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public void Withdraw_Should_Throw_When_Already_Approved()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Approve(_reviewerId);

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => request.Withdraw());
    }

    [Fact]
    public void Withdraw_Should_Throw_When_Already_Rejected()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Reject(_reviewerId);

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => request.Withdraw());
    }

    [Fact]
    public void Withdraw_Should_Throw_When_Already_Withdrawn()
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);
        request.Withdraw();

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => request.Withdraw());
    }

    #endregion

    #region Status Flow Tests

    [Theory]
    [InlineData(JoinRequestStatus.Approved)]
    [InlineData(JoinRequestStatus.Rejected)]
    [InlineData(JoinRequestStatus.Withdrawn)]
    public void All_Status_Changes_Should_Require_Pending_Status(JoinRequestStatus finalStatus)
    {
        // Arrange
        var request = ProviderJoinRequest.Create(_organizationId, _requesterId);

        // Act - change to final status
        switch (finalStatus)
        {
            case JoinRequestStatus.Approved:
                request.Approve(_reviewerId);
                break;
            case JoinRequestStatus.Rejected:
                request.Reject(_reviewerId);
                break;
            case JoinRequestStatus.Withdrawn:
                request.Withdraw();
                break;
        }

        // Assert - verify status
        Assert.Equal(finalStatus, request.Status);

        // Assert - all other status changes should fail
        Assert.Throws<DomainValidationException>(() => request.Approve(Guid.NewGuid()));
        Assert.Throws<DomainValidationException>(() => request.Reject(Guid.NewGuid()));
        Assert.Throws<DomainValidationException>(() => request.Withdraw());
    }

    #endregion
}
