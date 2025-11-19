// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetBookingHistory/BookingHistoryViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetBookingHistory
{
    /// <summary>
    /// View model for a booking history entry
    /// </summary>
    public sealed record BookingHistoryViewModel
    {
        public Guid BookingId { get; init; }
        public Guid ProviderId { get; init; }
        public string ProviderName { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public DateTime StartTime { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal? TotalPrice { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Paginated result for booking history
    /// </summary>
    public sealed record BookingHistoryResult
    {
        public List<BookingHistoryViewModel> Items { get; init; } = new();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
