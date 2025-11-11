using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;

/// <summary>
/// Query to get current registration progress and draft data
/// Used to resume registration flow
/// </summary>
public sealed record GetRegistrationProgressQuery() : IQuery<GetRegistrationProgressResult>;

public sealed record GetRegistrationProgressResult(
    bool HasDraft,
    int? CurrentStep,
    ProviderDraftData? DraftData
);

public sealed record ProviderDraftData(
    Guid ProviderId,
    int RegistrationStep,
    string Status,
    BusinessInfoData BusinessInfo,
    LocationData Location,
    List<ServiceData> Services,
    List<StaffData> Staff,
    List<BusinessHoursData> BusinessHours,
    List<GalleryImageData> GalleryImages
);

public sealed record BusinessInfoData(
    string BusinessName,
    string BusinessDescription,
    string Category,
    string PhoneNumber,
    string Email
);

public sealed record LocationData(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string PostalCode,
    decimal Latitude,
    decimal Longitude
);

public sealed record ServiceData(
    string Id,
    string Name,
    int DurationHours,
    int DurationMinutes,
    decimal Price,
    string PriceType
);

public sealed record StaffData(
    string Id,
    string Name,
    string Email,
    string PhoneNumber,
    string Position
);

public sealed record BusinessHoursData(
    int DayOfWeek,
    bool IsOpen,
    int? OpenTimeHours,
    int? OpenTimeMinutes,
    int? CloseTimeHours,
    int? CloseTimeMinutes,
    List<BreakPeriodData> Breaks
);

public sealed record BreakPeriodData(
    int StartTimeHours,
    int StartTimeMinutes,
    int EndTimeHours,
    int EndTimeMinutes,
    string? Label
);

public sealed record GalleryImageData(
    string ImageUrl,
    string? ThumbnailUrl,
    int DisplayOrder
);
