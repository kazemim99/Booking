public class CreateServiceRequest
{
    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    public ServiceType ServiceType { get; set; }

    [Required]
    [Range(0.01, 100000)]
    public decimal BasePrice { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(5, 1440)] // 5 minutes to 24 hours
    public int DurationMinutes { get; set; }

    [Range(0, 240)] // Up to 4 hours
    public int? PreparationMinutes { get; set; }

    [Range(0, 240)] // Up to 4 hours  
    public int? BufferMinutes { get; set; }

    public bool RequiresDeposit { get; set; } = false;

    [Range(0, 100)]
    public decimal DepositPercentage { get; set; } = 0;

    public bool AvailableAtLocation { get; set; } = true;

    public bool AvailableAsMobile { get; set; } = false;

    [Range(1, 365)]
    public int MaxAdvanceBookingDays { get; set; } = 90;

    [Range(1, 168)] // Up to 1 week in hours
    public int MinAdvanceBookingHours { get; set; } = 1;

    [Range(1, 10)]
    public int MaxConcurrentBookings { get; set; } = 1;

    public List<string>? Tags { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    public Guid? IdempotencyKey { get; set; }
}
