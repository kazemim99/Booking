namespace Booksy.ServiceCatalog.API.Models.Requests;

public class PriceCalculationRequest
{
    public List<Guid>? SelectedOptionIds { get; set; }
    public Guid? SelectedTierId { get; set; }
}