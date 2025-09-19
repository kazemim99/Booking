// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Service/ServiceAvailabilityDto.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Service
{
    public sealed class ServiceAvailabilityDto
    {
        public Guid Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsAvailable { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int MaxConcurrentBookings { get; set; }
        public List<Guid> AvailableStaff { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}