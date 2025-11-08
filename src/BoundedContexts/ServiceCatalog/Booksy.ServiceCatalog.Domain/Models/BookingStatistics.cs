// ========================================
// Booksy.ServiceCatalog.Domain/Models/BookingStatistics.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Models
{
    /// <summary>
    /// Domain model for booking statistics
    /// </summary>
    public sealed class BookingStatistics
    {
        public int TotalBookings { get; init; }
        public int RequestedBookings { get; init; }
        public int ConfirmedBookings { get; init; }
        public int CompletedBookings { get; init; }
        public int CancelledBookings { get; init; }
        public int NoShowBookings { get; init; }
        public int RescheduledBookings { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal CompletedRevenue { get; init; }
        public decimal PendingRevenue { get; init; }
        public decimal RefundedAmount { get; init; }
        public string Currency { get; init; } = "USD";
        public double CompletionRate { get; init; }
        public double NoShowRate { get; init; }
        public double CancellationRate { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }

        public BookingStatistics(
            int totalBookings,
            int requestedBookings,
            int confirmedBookings,
            int completedBookings,
            int cancelledBookings,
            int noShowBookings,
            int rescheduledBookings,
            decimal totalRevenue,
            decimal completedRevenue,
            decimal pendingRevenue,
            decimal refundedAmount,
            string currency,
            double completionRate,
            double noShowRate,
            double cancellationRate,
            DateTime? startDate,
            DateTime? endDate)
        {
            TotalBookings = totalBookings;
            RequestedBookings = requestedBookings;
            ConfirmedBookings = confirmedBookings;
            CompletedBookings = completedBookings;
            CancelledBookings = cancelledBookings;
            NoShowBookings = noShowBookings;
            RescheduledBookings = rescheduledBookings;
            TotalRevenue = totalRevenue;
            CompletedRevenue = completedRevenue;
            PendingRevenue = pendingRevenue;
            RefundedAmount = refundedAmount;
            Currency = currency;
            CompletionRate = completionRate;
            NoShowRate = noShowRate;
            CancellationRate = cancellationRate;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
