// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/BusinessHoursDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class BusinessHoursDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsOpen { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
    }
}