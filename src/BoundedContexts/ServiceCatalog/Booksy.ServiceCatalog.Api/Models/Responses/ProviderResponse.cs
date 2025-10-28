namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string Message { get; set; } = string.Empty;

    // Authentication tokens (only populated on registration)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }
}
