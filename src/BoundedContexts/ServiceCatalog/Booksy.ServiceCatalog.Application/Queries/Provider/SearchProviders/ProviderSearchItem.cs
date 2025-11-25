using Booksy.ServiceCatalog.Domain.Enums;

public sealed class ProviderSearchItem
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; }
    public string Description { get; init; }
    public ProviderType Type { get; init; }
    public ProviderStatus Status { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string Country { get; init; }
    public string? LogoUrl { get; init; }
    public bool AllowOnlineBooking { get; init; }
    public bool OffersMobileServices { get; init; }
    public decimal AverageRating { get; init; }
    // public int TotalReviews { get; init; }   // commented in record
    public int ServiceCount { get; init; }
    public int YearsInBusiness { get; init; }
    public bool IsVerified { get; init; }
    // public string? OperatingHours { get; init; } // commented in record
    public DateTime RegisteredAt { get; init; }
    public DateTime? LastActiveAt { get; init; }

    // Hierarchy information
    public ProviderHierarchyType HierarchyType { get; init; }
    public bool IsIndependent { get; init; }
    public Guid? ParentProviderId { get; init; }
    public string? ParentProviderName { get; init; }
    public int StaffProviderCount { get; init; }

    public ProviderSearchItem(
        Guid id,
        string businessName,
        string description,
        ProviderType type,
        ProviderStatus status,
        string city,
        string state,
        string country,
        string? logoUrl,
        bool allowOnlineBooking,
        bool offersMobileServices,
        decimal averageRating,
        int serviceCount,
        int yearsInBusiness,
        bool isVerified,
        DateTime registeredAt,
        DateTime? lastActiveAt,
        ProviderHierarchyType hierarchyType = ProviderHierarchyType.Organization,
        bool isIndependent = false,
        Guid? parentProviderId = null,
        string? parentProviderName = null,
        int staffProviderCount = 0)
    {
        Id = id;
        BusinessName = businessName;
        Description = description;
        Type = type;
        Status = status;
        City = city;
        State = state;
        Country = country;
        LogoUrl = logoUrl;
        AllowOnlineBooking = allowOnlineBooking;
        OffersMobileServices = offersMobileServices;
        AverageRating = averageRating;
        ServiceCount = serviceCount;
        YearsInBusiness = yearsInBusiness;
        IsVerified = isVerified;
        RegisteredAt = registeredAt;
        LastActiveAt = lastActiveAt;
        HierarchyType = hierarchyType;
        IsIndependent = isIndependent;
        ParentProviderId = parentProviderId;
        ParentProviderName = parentProviderName;
        StaffProviderCount = staffProviderCount;
    }
}
