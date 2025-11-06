// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetNotificationHistory/NotificationHistoryViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory
{
    /// <summary>
    /// View model for notification history
    /// </summary>
    public sealed record NotificationHistoryViewModel(
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages,
        List<NotificationHistoryItem> Notifications);

    public sealed record NotificationHistoryItem(
        Guid NotificationId,
        string Type,
        string Channel,
        string Status,
        string Subject,
        string Body,
        string Priority,
        string? RecipientEmail,
        string? RecipientPhone,
        DateTime CreatedAt,
        DateTime? SentAt,
        DateTime? DeliveredAt,
        DateTime? FailedAt,
        string? FailureReason,
        string? ExternalMessageId,
        int AttemptCount,
        Dictionary<string, string> Metadata)
    {
        public Guid RecipientId { get; set; }
    }
}
