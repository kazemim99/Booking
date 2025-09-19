
namespace Booksy.ServiceCatalog.API.Models.Responses;

public class PriceCalculationResponse
{
    public Guid ServiceId { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<PriceBreakdownItem> Breakdown { get; set; } = new();
}