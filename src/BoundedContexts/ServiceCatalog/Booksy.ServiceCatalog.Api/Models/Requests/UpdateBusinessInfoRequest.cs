using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request to update provider business information
/// </summary>
public sealed class UpdateBusinessInfoRequest
{
    [Required]
    [MaxLength(200)]
    public string BusinessName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }


    [Url]
    public string? LogoUrl { get; set; }
}
