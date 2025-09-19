public class UpdateServiceRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CategoryName { get; set; }

    [Required]
    [Range(5, 1440)] // 5 minutes to 24 hours
    public int DurationMinutes { get; set; }

    [Range(0, 240)] // Up to 4 hours
    public int? PreparationMinutes { get; set; }

    [Range(0, 240)] // Up to 4 hours
    public int? BufferMinutes { get; set; }

    public List<string>? Tags { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    public Guid? IdempotencyKey { get; set; }
}
