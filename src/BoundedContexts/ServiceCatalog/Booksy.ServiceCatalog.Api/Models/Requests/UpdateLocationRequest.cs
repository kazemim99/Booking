using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request to update provider location
/// </summary>
public sealed class UpdateLocationRequest
{
    [Required]
    public string FormattedAddress { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }

    public string? City { get; set; } = "";

    public string? PostalCode { get; set; } 

    public string Country { get; set; } = "Iran";

    public int? ProvinceId { get; set; }

    public int? CityId { get; set; }

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }
}
