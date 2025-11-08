// ========================================
// Booksy.ServiceCatalog.Domain/Enums/BookingStatusExtensions.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Extension methods for BookingStatus enum providing state transition validation
    /// </summary>
    public static class BookingStatusExtensions
    {
        /// <summary>
        /// Determines if a status transition is valid based on business rules
        /// </summary>
        /// <param name="currentStatus">Current booking status</param>
        /// <param name="targetStatus">Target status to transition to</param>
        /// <returns>True if transition is allowed, false otherwise</returns>
        public static bool CanTransitionTo(this BookingStatus currentStatus, BookingStatus targetStatus)
        {
            // Same status is always allowed (no transition)
            if (currentStatus == targetStatus)
                return true;

            return currentStatus switch
            {
                // Requested → Confirmed, Cancelled
                BookingStatus.Requested => targetStatus == BookingStatus.Confirmed ||
                                           targetStatus == BookingStatus.Cancelled ||
                                           targetStatus == BookingStatus.Rescheduled,

                // Confirmed → Completed, Cancelled, NoShow, Rescheduled
                BookingStatus.Confirmed => targetStatus == BookingStatus.Completed ||
                                           targetStatus == BookingStatus.Cancelled ||
                                           targetStatus == BookingStatus.NoShow ||
                                           targetStatus == BookingStatus.Rescheduled,

                // Terminal states (no transitions allowed)
                BookingStatus.Completed => false,
                BookingStatus.Cancelled => false,
                BookingStatus.NoShow => false,
                BookingStatus.Rescheduled => false,

                _ => false
            };
        }

        /// <summary>
        /// Gets all valid transition targets from the current status
        /// </summary>
        /// <param name="currentStatus">Current booking status</param>
        /// <returns>Collection of valid target statuses</returns>
        public static IEnumerable<BookingStatus> GetValidTransitions(this BookingStatus currentStatus)
        {
            return currentStatus switch
            {
                BookingStatus.Requested => new[]
                {
                    BookingStatus.Confirmed,
                    BookingStatus.Cancelled,
                    BookingStatus.Rescheduled
                },

                BookingStatus.Confirmed => new[]
                {
                    BookingStatus.Completed,
                    BookingStatus.Cancelled,
                    BookingStatus.NoShow,
                    BookingStatus.Rescheduled
                },

                // Terminal states have no valid transitions
                BookingStatus.Completed => Array.Empty<BookingStatus>(),
                BookingStatus.Cancelled => Array.Empty<BookingStatus>(),
                BookingStatus.NoShow => Array.Empty<BookingStatus>(),
                BookingStatus.Rescheduled => Array.Empty<BookingStatus>(),

                _ => Array.Empty<BookingStatus>()
            };
        }

        /// <summary>
        /// Determines if the status represents a terminal state (no further transitions)
        /// </summary>
        /// <param name="status">Booking status to check</param>
        /// <returns>True if status is terminal, false otherwise</returns>
        public static bool IsTerminal(this BookingStatus status)
        {
            return status == BookingStatus.Completed ||
                   status == BookingStatus.Cancelled ||
                   status == BookingStatus.NoShow ||
                   status == BookingStatus.Rescheduled;
        }

        /// <summary>
        /// Determines if the status represents an active booking
        /// </summary>
        /// <param name="status">Booking status to check</param>
        /// <returns>True if status is active, false otherwise</returns>
        public static bool IsActive(this BookingStatus status)
        {
            return status == BookingStatus.Requested ||
                   status == BookingStatus.Confirmed;
        }

        /// <summary>
        /// Gets a human-readable description of the status
        /// </summary>
        /// <param name="status">Booking status</param>
        /// <returns>Status description</returns>
        public static string GetDescription(this BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Requested => "Booking requested and awaiting confirmation",
                BookingStatus.Confirmed => "Booking confirmed and scheduled",
                BookingStatus.Cancelled => "Booking has been cancelled",
                BookingStatus.Completed => "Service completed successfully",
                BookingStatus.NoShow => "Customer did not show up",
                BookingStatus.Rescheduled => "Booking has been rescheduled to a new time",
                _ => "Unknown status"
            };
        }

        /// <summary>
        /// Gets the color representation for UI display
        /// </summary>
        /// <param name="status">Booking status</param>
        /// <returns>Color code (hex or name)</returns>
        public static string GetStatusColor(this BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Requested => "#FFA500", // Orange
                BookingStatus.Confirmed => "#28A745", // Green
                BookingStatus.Cancelled => "#DC3545", // Red
                BookingStatus.Completed => "#007BFF", // Blue
                BookingStatus.NoShow => "#6C757D",    // Gray
                BookingStatus.Rescheduled => "#17A2B8", // Cyan
                _ => "#000000" // Black
            };
        }
    }
}
