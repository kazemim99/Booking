// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/BusinessHoursDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    /// <summary>
    /// Business hours for a single day of the week
    /// </summary>
    public sealed class BusinessHoursDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsOpen { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }

        /// <summary>
        /// Break periods within the business hours
        /// </summary>
        public List<BreakPeriodDto> Breaks { get; set; } = new();
    }

    /// <summary>
    /// Break period within business hours
    /// </summary>
    public sealed class BreakPeriodDto
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}