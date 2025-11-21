// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/ProviderServiceItem.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    /// <summary>
    /// Service summary item in GetProviderById result
    /// </summary>
    public sealed class ProviderServiceItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Duration { get; set; }
        public ServiceStatus Status { get; set; }
        public Guid ProviderId { get; set; }
        public string Type { get; set; }
        public string ImageUrl { get; set; }
    }
}