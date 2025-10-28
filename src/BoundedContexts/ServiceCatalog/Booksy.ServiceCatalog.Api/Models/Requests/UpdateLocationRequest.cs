using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request to update provider location
/// </summary>
public sealed class UpdateLocationRequest
{
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    [Required]
    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    [Required]
    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = "Iran";

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public string? FormattedAddress { get; set; }

    public bool IsShared { get; set; }
    public string Street { get; set; }
}
