// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
{
    public sealed record GetAvailableSlotsResult(
        Guid ProviderId,
        Guid ServiceId,
        DateTime Date,
        List<TimeSlotDto> AvailableSlots,
        List<string>? ValidationMessages = null);

    public sealed record TimeSlotDto(
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        Guid StaffId,
        string StaffName)
    {
        public bool IsAvailable { get; set; }
        public Guid? AvailableStaffId { get; set; }
        public string AvailableStaffName { get; set; }
    }
}
