namespace Booksy.ServiceCatalog.API.Models.Responses;

public class AddressResponse
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}







public sealed class ServiceSearchResultResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresDeposit { get; set; }
    public bool AvailableAsMobile { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public ProviderInfoResponse Provider { get; set; } = new();
}

public sealed class ProviderInfoResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool AllowOnlineBooking { get; set; }
    public bool OffersMobileServices { get; set; }
}

public sealed class SearchFiltersResponse
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public string? Type { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MaxDurationMinutes { get; set; }
    public bool? AvailableAsMobile { get; set; }
    public string? Location { get; set; }
}