using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ProviderInvitationAggregate;

/// <summary>
/// Unit tests for ProviderInvitation aggregate
/// Tests the invitation workflow for organizations inviting individuals
/// </summary>
public class ProviderInvitationTests
{
    private readonly ProviderId _organizationId = ProviderId.New();
    private readonly PhoneNumber _phoneNumber = PhoneNumber.Create("+989123456789");

    #region Create Tests

    [Fact]
    public void Create_Should_Create_Invitation_With_Pending_Status()
    {
        // Arrange & Act
        var invitation = ProviderInvitation.Create(
            _organizationId,
            _phoneNumber,
            "John Doe",
            "Welcome to our salon!");

        // Assert
        Assert.NotEqual(Guid.Empty, invitation.Id);
        Assert.Equal(_organizationId, invitation.OrganizationId);
        Assert.Equal(_phoneNumber.Value, invitation.PhoneNumber.Value);
        Assert.Equal("John Doe", invitation.InviteeName);
        Assert.Equal("Welcome to our salon!", invitation.Message);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.True((DateTime.UtcNow - invitation.CreatedAt).TotalSeconds < 5);
        Assert.True((invitation.ExpiresAt - DateTime.UtcNow).TotalDays >= 6);
    }

    [Fact]
    public void Create_Should_Set_Default_Expiration_Of_7_Days()
    {
        // Arrange & Act
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Assert
        var expectedExpiration = DateTime.UtcNow.AddDays(7);
        Assert.True(Math.Abs((invitation.ExpiresAt - expectedExpiration).TotalMinutes) < 1);
    }

    [Fact]
    public void Create_Should_Allow_Custom_Expiration_Days()
    {
        // Arrange & Act
        var invitation = ProviderInvitation.Create(
            _organizationId,
            _phoneNumber,
            expirationDays: 14);

        // Assert
        var expectedExpiration = DateTime.UtcNow.AddDays(14);
        Assert.True(Math.Abs((invitation.ExpiresAt - expectedExpiration).TotalMinutes) < 1);
    }

    [Fact]
    public void Create_Should_Raise_InvitationSent_Event()
    {
        // Arrange & Act
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Assert
        var domainEvents = invitation.DomainEvents;
        Assert.Contains(domainEvents, e => e is InvitationSentEvent);

        var sentEvent = domainEvents.OfType<InvitationSentEvent>().First();
        Assert.Equal(invitation.Id, sentEvent.InvitationId);
        Assert.Equal(_organizationId, sentEvent.OrganizationId);
    }

    [Fact]
    public void Create_Should_Work_Without_Optional_Parameters()
    {
        // Arrange & Act
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Assert
        Assert.Null(invitation.InviteeName);
        Assert.Null(invitation.Message);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    #endregion

    #region Accept Tests

    [Fact]
    public void Accept_Should_Change_Status_To_Accepted()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        var individualProviderId = ProviderId.New();

        // Act
        invitation.Accept(individualProviderId);

        // Assert
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(individualProviderId, invitation.AcceptedByProviderId);
        Assert.NotNull(invitation.RespondedAt);
        Assert.True((DateTime.UtcNow - invitation.RespondedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Accept_Should_Raise_InvitationAccepted_Event()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        var individualProviderId = ProviderId.New();

        // Act
        invitation.Accept(individualProviderId);

        // Assert
        var domainEvents = invitation.DomainEvents;
        Assert.Contains(domainEvents, e => e is InvitationAcceptedEvent);

        var acceptedEvent = domainEvents.OfType<InvitationAcceptedEvent>().First();
        Assert.Equal(invitation.Id, acceptedEvent.InvitationId);
        Assert.Equal(_organizationId, acceptedEvent.OrganizationId);
        Assert.Equal(individualProviderId, acceptedEvent.IndividualProviderId);
    }

    [Fact]
    public void Accept_Should_Throw_When_Already_Accepted()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        var individualProviderId = ProviderId.New();
        invitation.Accept(individualProviderId);

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            invitation.Accept(ProviderId.New()));
        Assert.Contains("Accepted", exception.Message);
    }

    [Fact]
    public void Accept_Should_Throw_When_Rejected()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Reject();

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            invitation.Accept(ProviderId.New()));
        Assert.Contains("Rejected", exception.Message);
    }

    [Fact]
    public void Accept_Should_Throw_When_Cancelled()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Cancel();

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            invitation.Accept(ProviderId.New()));
        Assert.Contains("Cancelled", exception.Message);
    }

    #endregion

    #region Reject Tests

    [Fact]
    public void Reject_Should_Change_Status_To_Rejected()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Act
        invitation.Reject();

        // Assert
        Assert.Equal(InvitationStatus.Rejected, invitation.Status);
        Assert.NotNull(invitation.RespondedAt);
    }

    [Fact]
    public void Reject_Should_Throw_When_Not_Pending()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Accept(ProviderId.New());

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => invitation.Reject());
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_Should_Change_Status_To_Cancelled()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Act
        invitation.Cancel();

        // Assert
        Assert.Equal(InvitationStatus.Cancelled, invitation.Status);
        Assert.NotNull(invitation.RespondedAt);
    }

    [Fact]
    public void Cancel_Should_Throw_When_Not_Pending()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Reject();

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => invitation.Cancel());
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_Should_Return_True_For_Pending_Non_Expired_Invitation()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Act & Assert
        Assert.True(invitation.IsValid());
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Accepted_Invitation()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Accept(ProviderId.New());

        // Act & Assert
        Assert.False(invitation.IsValid());
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Rejected_Invitation()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Reject();

        // Act & Assert
        Assert.False(invitation.IsValid());
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Cancelled_Invitation()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);
        invitation.Cancel();

        // Act & Assert
        Assert.False(invitation.IsValid());
    }

    #endregion

    #region MarkAsExpired Tests

    [Fact]
    public void MarkAsExpired_Should_Not_Change_Status_If_Not_Expired_Yet()
    {
        // Arrange
        var invitation = ProviderInvitation.Create(_organizationId, _phoneNumber);

        // Act
        invitation.MarkAsExpired();

        // Assert - Status should remain Pending since it hasn't expired
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    #endregion
}
