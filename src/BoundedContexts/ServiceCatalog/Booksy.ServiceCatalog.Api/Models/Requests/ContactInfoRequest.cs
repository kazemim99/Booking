//===========================================
// Models/Requests/RegisterProviderRequest.cs
//===========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

public class ContactInfoRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PrimaryPhone { get; set; } = string.Empty;

    [Phone]
    public string? SecondaryPhone { get; set; }

    [Url]
    public string? Website { get; set; }
}
