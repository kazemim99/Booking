using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderDetailsResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ContactInfoResponse ContactInfo { get; set; } = new();
    public AddressResponse Address { get; set; } = new();
    public IEnumerable<BusinessHoursData>? BusinessHours { get; set; }
    public bool AllowOnlineBooking { get; set; }
    public bool OffersMobileServices { get; set; }
    public string? LogoUrl { get; set; }
    public List<string>? Tags { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public Guid OwnerId { get; internal set; }
    public string? WebsiteUrl { get; internal set; }
    public bool IsVerified { get; internal set; }
    public int TotalReviews { get; internal set; }
    public decimal AverageRating { get; internal set; }
    public int ServiceCount { get; internal set; }
    public int YearsInBusiness { get; internal set; }
    public DateTime? ActivatedAt { get; internal set; }
    public List<ServiceSummaryResponse> Services { get; internal set; }
    public List<StaffMemberResponse>? Staff { get; internal set; }
    public string? ProfileImageUrl { get;  set; }

    // Hierarchy information
    /// <summary>
    /// The provider's hierarchy type (Organization or Individual)
    /// </summary>
    public string? HierarchyType { get; set; }

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
    /// Staff members as individual providers (for organizations)
    /// </summary>
    public List<StaffProviderResponse>? StaffProviders { get; set; }
}
