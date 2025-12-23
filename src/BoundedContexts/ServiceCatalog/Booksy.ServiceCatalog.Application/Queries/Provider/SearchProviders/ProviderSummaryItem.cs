
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed record ProviderSummaryItem(
        Guid Id,
        string BusinessName,
        string Description,
        ServiceCategory PrimaryCategory,
        ProviderStatus Status,
        string City,
        string State,
        string Country,
        string? LogoUrl,
        bool AllowOnlineBooking,
        bool OffersMobileServices,
        bool IsVerified,
        decimal AverageRating,
        int TotalReviews,
        int ServiceCount,
        DateTime RegisteredAt);
}
