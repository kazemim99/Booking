// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Service/ServiceSummaryDto.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Service
{
    public sealed class ServiceSummaryDto
    {
        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ServiceType Type { get; set; }
        public decimal BasePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Duration { get; set; }
        public ServiceStatus Status { get; set; }
        public bool RequiresDeposit { get; set; }
        public bool AvailableAsMobile { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool CanBeBooked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}