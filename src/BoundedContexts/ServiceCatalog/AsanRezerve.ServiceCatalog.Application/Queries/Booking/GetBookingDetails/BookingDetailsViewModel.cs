// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/BookingDetailsViewModel.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    public sealed record BookingDetailsViewModel(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        string ProviderName,
        Guid ServiceId,
        string ServiceName,
        Guid StaffId,
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
        List<BookingHistoryDto> History,
        // Same rule as CustomerBookingDto.StaffName.
        string? StaffName = null,
        // Same rule as CustomerBookingDto.RescheduleBlockedReason.
        string? RescheduleBlockedReason = null,
        // The caller is the person this booking is for: its own customer, or — for a booking the salon entered — the
        // person with its client-book entry's verified mobile (IBookingCustomer). Lets that person open it.
        bool IsForCaller = false,
        // Same rule as CustomerBookingDto's review fields; empty unless IsForCaller.
        bool CanReview = false,
        string? ReviewBlockedReason = null,
        Guid? ReviewId = null,
        string? ReviewStatus = null,
        bool ReviewEditable = false,
        Guid? ReviewBookingId = null);

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
