//===========================================
// Supporting Request/Response Models and Extensions
//===========================================

//===========================================
// Models/Requests/SearchServicesRequest.cs
//===========================================
//===========================================
// Models/Responses/ServiceSearchResponse.cs
//===========================================
using Booksy.ServiceCatalog.API.Models.Responses;

namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    public sealed class ServiceSearchResponse
    {
        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool RequiresDeposit { get; set; }
        public bool AvailableAsMobile { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public ProviderInfoResponse Provider { get; set; } = new();
    }
}
