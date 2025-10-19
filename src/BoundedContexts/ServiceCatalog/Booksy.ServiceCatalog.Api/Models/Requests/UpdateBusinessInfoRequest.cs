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

    [Required]
    [MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }
}
