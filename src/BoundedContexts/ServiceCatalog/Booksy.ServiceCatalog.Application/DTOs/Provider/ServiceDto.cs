// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Service/ServiceDto.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Service
{
    public sealed class ServiceDto
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
        public int? PreparationTime { get; set; }
        public int? BufferTime { get; set; }
        public ServiceStatus Status { get; set; }
        public bool RequiresDeposit { get; set; }
        public decimal DepositPercentage { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool AvailableAtLocation { get; set; }
        public bool AvailableAsMobile { get; set; }
        public int MaxAdvanceBookingDays { get; set; }
        public int MinAdvanceBookingHours { get; set; }
        public int MaxConcurrentBookings { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<ServiceOptionDto> Options { get; set; } = new();
        public List<PriceTierDto> PriceTiers { get; set; } = new();
        public List<Guid> QualifiedStaff { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}