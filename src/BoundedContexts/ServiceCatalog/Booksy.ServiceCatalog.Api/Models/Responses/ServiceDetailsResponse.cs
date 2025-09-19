
namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ServiceDetailsResponse
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
    public int? PreparationTime { get; set; }
    public int? BufferTime { get; set; }
    public int TotalDuration { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresDeposit { get; set; }
    public decimal DepositPercentage { get; set; }
    public decimal DepositAmount { get; set; }
    public bool AllowOnlineBooking { get; set; }
    public bool AvailableAtLocation { get; set; }
    public bool AvailableAsMobile { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public int MinAdvanceBookingHours { get; set; }
    public int MaxConcurrentBookings { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public int QualifiedStaffCount { get; set; }
    public bool CanBeBooked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public List<ServiceOptionResponse>? Options { get; set; }
    public List<PriceTierResponse>? PriceTiers { get; set; }
    public ProviderSummaryResponse? Provider { get; set; }
}
