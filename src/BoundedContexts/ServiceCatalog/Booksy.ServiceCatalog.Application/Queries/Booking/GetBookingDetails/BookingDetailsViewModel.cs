// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/BookingDetailsViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    public sealed record BookingDetailsViewModel(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        string ProviderName,
        Guid ServiceId,
        string ServiceName,
        Guid StaffId,
        string StaffName,
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        string Status,
        decimal TotalPrice,
        string Currency,
        PaymentInfoDto PaymentInfo,
        string? CustomerNotes,
        string? StaffNotes,
        string? CancellationReason,
        DateTime RequestedAt,
        DateTime? ConfirmedAt,
        DateTime? CancelledAt,
        DateTime? CompletedAt,
        List<BookingHistoryDto> History);

    public sealed record PaymentInfoDto(
        decimal TotalAmount,
        decimal DepositAmount,
        decimal PaidAmount,
        decimal RefundedAmount,
        string Status,
        bool IsFullyPaid,
        decimal RemainingAmount);

    public sealed record BookingHistoryDto(
        string Description,
        string Status,
        DateTime OccurredAt);
}
