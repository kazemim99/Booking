using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitationWithRegistration;

/// <summary>
/// Command to accept an invitation with quick registration for unregistered users
/// This command handles:
/// - OTP verification
/// - User account creation
/// - Individual provider profile creation
/// - Profile data cloning (services, working hours, gallery)
/// - Invitation acceptance
/// - JWT token generation
/// </summary>
public sealed record AcceptInvitationWithRegistrationCommand(
    Guid InvitationId,
    Guid OrganizationId,
    string PhoneNumber,
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery,
    Guid? IdempotencyKey = null) : ICommand<AcceptInvitationWithRegistrationResult>;

public sealed record AcceptInvitationWithRegistrationResult(
    Guid UserId,
    Guid ProviderId,
    string AccessToken,
    string RefreshToken,
    int ClonedServicesCount,
    int ClonedWorkingHoursCount,
    int ClonedGalleryCount,
    DateTime AcceptedAt);
