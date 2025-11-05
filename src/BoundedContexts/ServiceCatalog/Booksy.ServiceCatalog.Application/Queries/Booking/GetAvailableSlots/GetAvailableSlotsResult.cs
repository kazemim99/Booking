// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
{
    public sealed record GetAvailableSlotsResult(
        Guid ProviderId,
        Guid ServiceId,
        DateTime Date,
        List<TimeSlotDto> AvailableSlots);

    public sealed record TimeSlotDto(
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        Guid StaffId,
        string StaffName);
}
