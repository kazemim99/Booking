//===========================================
// Models/Requests/RegisterProviderRequest.cs
//===========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

public class AddressRequest
{
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }
}
