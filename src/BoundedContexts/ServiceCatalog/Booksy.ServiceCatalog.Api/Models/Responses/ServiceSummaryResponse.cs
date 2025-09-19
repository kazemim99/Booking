namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ServiceSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<string> Tags { get; set; } = new();
}
