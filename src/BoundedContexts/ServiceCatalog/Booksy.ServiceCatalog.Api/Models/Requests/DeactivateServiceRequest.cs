namespace Booksy.ServiceCatalog.API.Models.Requests;

public class DeactivateServiceRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}
