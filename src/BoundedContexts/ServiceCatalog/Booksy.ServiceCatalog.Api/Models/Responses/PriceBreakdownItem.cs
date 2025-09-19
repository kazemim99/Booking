namespace Booksy.ServiceCatalog.API.Models.Responses;

public class PriceBreakdownItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}