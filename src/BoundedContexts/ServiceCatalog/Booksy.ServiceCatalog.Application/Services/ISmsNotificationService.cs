// ========================================
// Booksy.ServiceCatalog.Application/Services/ISmsNotificationService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services
{
    /// <summary>
    /// Service for sending SMS notifications
    /// </summary>
    public interface ISmsNotificationService
    {
        /// <summary>
        /// Sends a booking confirmation SMS
        /// </summary>
        Task SendBookingConfirmedSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking created SMS
        /// </summary>
        Task SendBookingCreatedSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking cancelled SMS
        /// </summary>
        Task SendBookingCancelledSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking rescheduled SMS
        /// </summary>
        Task SendBookingRescheduledSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime oldTime,
            DateTime newTime,
            string providerName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking completed SMS with review request
        /// </summary>
        Task SendBookingCompletedSmsAsync(
            string phoneNumber,
            string customerName,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking reminder SMS
        /// </summary>
        Task SendBookingReminderSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceAddress,
            CancellationToken cancellationToken = default);
    }
}
