using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;

/// <summary>
/// Result for GetProviderById query - contains complete provider details
/// </summary>
public sealed class ProviderDetailsResult
{
    public ProviderDetailsResult()
    {
        Staff = new List<ProviderStaffItem>();
        StaffProviders = new List<StaffProviderInfo>();
    }

    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProviderType Type { get; init; }
    public ProviderStatus Status { get; init; }
    public Booksy.ServiceCatalog.Application.DTOs.Provider.ContactInfo ContactInfo { get; init; }
    public AddressInfo Address { get; init; }
    public IEnumerable<BusinessHoursData> BusinessHours { get; init; }
    public string? LogoUrl { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool AllowOnlineBooking { get; init; }
    public bool OffersMobileServices { get; init; }
    public bool IsVerified { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int ServiceCount { get; init; }
    public int StaffCount { get; init; }
    public int YearsInBusiness { get; init; }
    public IReadOnlyList<string> Tags { get; init; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? LastActiveAt { get; init; }
    public List<ProviderServiceItem>? Services { get; set; }
    public List<ProviderStaffItem> Staff { get; internal set; }
    public int ActiveServicesCount { get; internal set; }

    // Hierarchy information
    public ProviderHierarchyType HierarchyType { get; init; }
    public bool IsIndependent { get; init; }
    public Guid? ParentProviderId { get; init; }
    public ParentProviderInfo? ParentProvider { get; set; }
    public List<StaffProviderInfo> StaffProviders { get; set; }
    public int StaffProviderCount { get; set; }
}

/// <summary>
/// Summary information about the parent organization
/// </summary>
public sealed class ParentProviderInfo
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public ProviderStatus Status { get; init; }
}

/// <summary>
/// Summary information about a staff provider (individual linked to organization)
/// </summary>
public sealed class StaffProviderInfo
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public ProviderStatus Status { get; init; }
    public bool IsIndependent { get; init; }
    public decimal AverageRating { get; init; }
    public int ServiceCount { get; init; }
}
