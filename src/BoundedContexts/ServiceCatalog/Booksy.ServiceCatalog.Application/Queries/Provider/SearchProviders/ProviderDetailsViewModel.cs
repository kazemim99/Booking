
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;

public sealed class ProviderDetailsViewModel
{

    public ProviderDetailsViewModel()
    {
        Staff = new List<StaffViewModel>();
    }
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProviderType Type { get; init; }
    public ProviderStatus Status { get; init; }
    public Booksy.ServiceCatalog.Application.DTOs.Provider.ContactInfo ContactInfo { get; init; }
    public AddressInfo Address { get; init; }
    public Dictionary<DayOfWeek, BusinessHoursDto?> BusinessHours { get; init; }
    public string? LogoUrl { get; init; }
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
    public List<ServiceSummaryViewModel>? Services { get; set; }

    public List<StaffViewModel> Staff { get; internal set; }
    public int ActiveServicesCount { get; internal set; }


}
