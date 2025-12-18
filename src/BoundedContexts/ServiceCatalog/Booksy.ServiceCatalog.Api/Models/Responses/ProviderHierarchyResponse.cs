namespace Booksy.ServiceCatalog.API.Models.Responses;

/// <summary>
/// Response DTO for provider hierarchy information
/// </summary>
public class ProviderHierarchyResponse
{
    /// <summary>
    /// The provider's hierarchy type (Organization or Individual)
    /// </summary>
    public string HierarchyType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is independent (not linked to any organization)
    /// </summary>
    public bool IsIndependent { get; set; }

    /// <summary>
    /// Parent organization ID if this is a linked individual provider
    /// </summary>
    public Guid? ParentProviderId { get; set; }

    /// <summary>
    /// Parent organization details (if this is a linked individual)
    /// </summary>
    public ParentOrganizationResponse? ParentOrganization { get; set; }

    /// <summary>
    /// Number of staff members (if this is an organization)
    /// </summary>
    public int StaffCount { get; set; }

    /// <summary>
    /// Whether this provider can accept direct bookings
    /// </summary>
    public bool CanAcceptDirectBookings { get; set; }

    /// <summary>
    /// Whether this provider can have staff members
    /// </summary>
    public bool CanHaveStaff { get; set; }
}

/// <summary>
/// Summary information about a parent organization
/// </summary>
public class ParentOrganizationResponse
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary information about a staff member (individual provider linked to organization)
/// </summary>
public class StaffProviderResponse
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsIndependent { get; set; }
    public DateTime JoinedAt { get; set; }
    public decimal AverageRating { get; set; }
    public int ServiceCount { get; set; }
}
