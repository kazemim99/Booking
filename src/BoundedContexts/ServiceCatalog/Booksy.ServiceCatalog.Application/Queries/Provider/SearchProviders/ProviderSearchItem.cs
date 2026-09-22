using Booksy.ServiceCatalog.Domain.Enums;

public sealed class ProviderSearchItem
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string Description { get; init; }
    public ServiceCategory PrimaryCategory { get; init; }
    public ProviderStatus Status { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string Country { get; init; }
    public string? LogoUrl { get; init; }
    public bool AllowOnlineBooking { get; init; }
    public bool OffersMobileServices { get; init; }
    /// <summary>0 when <see cref="TotalReviews"/> is 0 — read the two together.</summary>
    public decimal AverageRating { get; init; }

    /// <summary>Published reviews behind <see cref="AverageRating"/>. 0 means "no reviews yet", not "rated zero".</summary>
    public int TotalReviews { get; init; }
    public int ServiceCount { get; init; }
    public int YearsInBusiness { get; init; }
    public bool IsVerified { get; init; }
    // public string? OperatingHours { get; init; } // commented in record
    public DateTime RegisteredAt { get; init; }
    public DateTime? LastActiveAt { get; init; }

    /// <summary>How many members of this salon currently take bookings.</summary>
    public int StaffMemberCount { get; init; }

    public ProviderSearchItem(
        Guid id,
        string businessName,
        string description,
        string? profileImageUrl,
        ServiceCategory primaryCategory,
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
        int staffMemberCount = 0,
        int totalReviews = 0)
    {
        Id = id;
        BusinessName = businessName;
        ProfileImageUrl = profileImageUrl;
        Description = description;
        PrimaryCategory = primaryCategory;
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
        StaffMemberCount = staffMemberCount;
        TotalReviews = totalReviews;
    }
}
