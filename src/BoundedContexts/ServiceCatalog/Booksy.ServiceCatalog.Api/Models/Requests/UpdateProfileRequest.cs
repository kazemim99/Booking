namespace Booksy.ServiceCatalog.Api.Models.Requests;

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
}
