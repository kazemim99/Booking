namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ContactInfoResponse
{
    public string Email { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? SecondaryPhone { get; set; }
    public string? Website { get; set; }
}
