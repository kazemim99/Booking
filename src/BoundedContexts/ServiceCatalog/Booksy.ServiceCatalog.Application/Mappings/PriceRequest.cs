// ========================================
// Booksy.ServiceCatalog.Application/Mappings/ServiceCatalogMappingExtensions.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Mappings
{
    public class PriceRequest
    {
        public string Currency { get; internal set; }
        public decimal Amount { get; internal set; }
    }
}