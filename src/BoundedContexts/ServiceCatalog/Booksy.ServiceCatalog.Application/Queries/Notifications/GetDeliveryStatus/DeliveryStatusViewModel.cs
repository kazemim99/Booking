// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetDeliveryStatus/DeliveryStatusViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus
{
    /// <summary>
    /// View model for notification delivery status
    /// </summary>
    public sealed record DeliveryStatusViewModel(
        Guid NotificationId,
        string Status,
        string Channel,
        string Type,
        string Priority,
        DateTime CreatedAt,
        DateTime? QueuedAt,
        DateTime? ScheduledFor,
        DateTime? SentAt,
        DateTime? DeliveredAt,
        DateTime? FailedAt,
        string? FailureReason,
        string? ExternalMessageId,
        int AttemptCount,
        int MaxRetryAttempts,
        bool CanRetry,
        bool IsExpired,
        DateTime? ExpiresAt,
        DateTime? LastAttemptAt,
        DateTime? NextRetryAt,
        List<DeliveryAttemptDto> DeliveryAttempts);

    public sealed record DeliveryAttemptDto(
        DateTime AttemptedAt,
        string Status,
        string? ErrorMessage,
        string? ExternalMessageId,
        TimeSpan? ResponseTime);
}
