// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    /// <summary>
    /// Service for handling integration with other bounded contexts
    /// </summary>
    public interface IServiceCatalogIntegrationService
    {
        // User Management Integration
        Task SyncProviderWithUserManagementAsync(UserId userId, string email, string firstName, string lastName, CancellationToken cancellationToken = default);
        Task HandleUserDeactivationAsync(UserId userId, CancellationToken cancellationToken = default);
        Task ValidateProviderOwnershipAsync(ProviderId providerId, UserId userId, CancellationToken cancellationToken = default);

        // Booking System Integration
        Task SyncServiceWithBookingSystemAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
        Task HandleBookingCompletedAsync(Guid bookingId, ServiceId serviceId, decimal amount, string currency, CancellationToken cancellationToken = default);
        Task NotifyProviderOfBookingAsync(ProviderId providerId, Guid bookingId, string customerInfo, CancellationToken cancellationToken = default);
        Task<bool> ValidateServiceAvailabilityAsync(ServiceId serviceId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        // Reviews Integration
        Task UpdateServiceReputationAsync(ServiceId serviceId, double newRating, int totalReviews, CancellationToken cancellationToken = default);
        Task SyncProviderReputationAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        // Schedule Synchronization
        Task SyncProviderScheduleAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task HandleStaffScheduleChangeAsync(ProviderId providerId, Guid staffId, CancellationToken cancellationToken = default);

        // Notification Integration
        Task SendProviderActivationNotificationAsync(ProviderId providerId, CancellationToken cancellationToken = default);
        Task SendServiceActivationNotificationAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
        Task SendBusinessHoursUpdateNotificationAsync(ProviderId providerId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
    }
}
