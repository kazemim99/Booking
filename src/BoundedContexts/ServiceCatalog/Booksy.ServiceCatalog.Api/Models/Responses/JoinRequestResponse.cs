namespace Booksy.ServiceCatalog.API.Models.Responses;

/// <summary>
/// Response DTO for a provider join request
/// </summary>
public class JoinRequestResponse
{
    /// <summary>
    /// Unique identifier for the join request
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The organization being requested to join
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// The provider requesting to join
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// Requester's business name
    /// </summary>
    public string RequesterName { get; set; } = string.Empty;

    /// <summary>
    /// Requester's profile image URL
    /// </summary>
    public string? RequesterProfileImageUrl { get; set; }

    /// <summary>
    /// Optional message from the requester
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Current status of the request (Pending, Approved, Rejected)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the request was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was reviewed (approved or rejected)
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// ID of the user who reviewed the request
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Note provided by reviewer when approving/rejecting
    /// </summary>
    public string? ReviewNote { get; set; }
}

/// <summary>
/// Response DTO for a provider invitation
/// </summary>
public class InvitationResponse
{
    /// <summary>
    /// Unique identifier for the invitation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The organization sending the invitation
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number the invitation was sent to
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name of the invitee (if provided)
    /// </summary>
    public string? InviteeName { get; set; }

    /// <summary>
    /// Optional message included with the invitation
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Current status (Pending, Accepted, Expired, Rejected)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the invitation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the invitation expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the invitation was accepted (if accepted)
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Provider ID that accepted the invitation (if accepted)
    /// </summary>
    public Guid? AcceptedByProviderId { get; set; }
}
