namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
