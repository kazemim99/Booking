using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;


public class PricingTierRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinQuantity { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxQuantity { get; set; }
}
