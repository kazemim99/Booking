using AsanRezerve.API.Extensions;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.CancelNotification;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.ResendNotification;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.MarkRead;
using AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus;
using AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetInbox;
using AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics;
using AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsanRezerve.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages notification operations for users
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ISender mediator,
        ILogger<NotificationsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a notification to a user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SendNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendNotificationCommand(
            request.RecipientId,
            request.Type,
            request.Channel,
            request.Subject,
            request.Body,
            request.Priority,
            request.PlainTextBody,
            request.RecipientEmail,
            request.RecipientPhone);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    [HttpPost("schedule")]
    [ProducesResponseType(typeof(ScheduleNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleNotification(
        [FromBody] ScheduleNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleNotificationCommand(
            request.RecipientId,
            request.Type,
            request.Channel,
            request.Subject,
            request.Body,
            request.ScheduledFor,
            request.Priority,
            request.PlainTextBody,
            request.RecipientEmail,
            request.RecipientPhone);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Send bulk notifications to multiple users
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Provider")]
    [ProducesResponseType(typeof(SendBulkNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendBulkNotification(
        [FromBody] SendBulkNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendBulkNotificationCommand(
            request.RecipientIds,
            request.Type,
            request.Channel,
            request.Subject,
            request.Body,
            request.Priority,
            request.PlainTextBody);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a pending notification
    /// </summary>
    [HttpPost("{notificationId}/cancel")]
    [ProducesResponseType(typeof(CancelNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelNotification(
        Guid notificationId,
        [FromBody] CancelNotificationRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CancelNotificationCommand(notificationId, request?.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resend a failed notification
    /// </summary>
    [HttpPost("{notificationId}/resend")]
    [ProducesResponseType(typeof(ResendNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendNotification(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var command = new ResendNotificationCommand(notificationId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get notification history for the current user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(NotificationHistoryViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] NotificationChannel? channel = null,
        [FromQuery] NotificationType? type = null,
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var query = new GetNotificationHistoryQuery(
            userId,
            channel,
            type,
            status,
            startDate,
            endDate,
            pageNumber,
            pageSize);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get delivery status of a specific notification
    /// </summary>
    [HttpGet("{notificationId}/status")]
    [ProducesResponseType(typeof(DeliveryStatusViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeliveryStatus(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var query = new GetDeliveryStatusQuery(notificationId, User.GetUserId());
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound($"Notification {notificationId} not found");

        return Ok(result);
    }

    /// <summary>
    /// Get notification analytics for the current user
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(NotificationAnalyticsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var query = new GetNotificationAnalyticsQuery(userId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// The caller's own notifications, newest first.
    /// </summary>
    /// <remarks>
    /// Each row carries its event code, so the app picks its icon and layout from that rather than from
    /// translated text, plus an isActionable flag recomputed for this read — a notification about a booking
    /// that has since been deleted still displays, but must not be tappable.
    /// </remarks>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(InboxPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInbox(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInboxQuery(User.GetUserId(), pageNumber, pageSize, unreadOnly),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>How many notifications the caller has not opened — the badge count.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new GetUnreadCountQuery(User.GetUserId()), cancellationToken);
        return Ok(new { unreadCount = count });
    }

    /// <summary>Marks one notification read. Idempotent.</summary>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var marked = await _mediator.Send(
            new MarkNotificationReadCommand(notificationId, User.GetUserId()),
            cancellationToken);

        // Someone else's notification answers as not-found, so this cannot be used to probe which ids exist.
        return marked ? NoContent() : NotFound();
    }

    /// <summary>Marks everything the caller has as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new MarkAllNotificationsReadCommand(User.GetUserId()), cancellationToken);
        return Ok(new { markedRead = count });
    }

}

// Request DTOs
public record SendNotificationRequest(
    Guid RecipientId,
    NotificationType Type,
    NotificationChannel Channel,
    string Subject,
    string Body,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? PlainTextBody = null,
    string? RecipientEmail = null,
    string? RecipientPhone = null,
    Guid? BookingId = null,
    Guid? ProviderId = null,
    Guid? PaymentId = null,
    Dictionary<string, string>? Metadata = null,
    string? TemplateKey = null,
    Dictionary<string, object>? TemplateVariables = null);

public record ScheduleNotificationRequest(
    Guid RecipientId,
    NotificationType Type,
    NotificationChannel Channel,
    string Subject,
    string Body,
    DateTime ScheduledFor,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? PlainTextBody = null,
    string? RecipientEmail = null,
    string? RecipientPhone = null,
    Guid? BookingId = null,
    Guid? ProviderId = null,
    Guid? PaymentId = null,
    Dictionary<string, string>? Metadata = null,
    string? TemplateKey = null,
    Dictionary<string, object>? TemplateVariables = null);

public record SendBulkNotificationRequest(
    List<Guid> RecipientIds,
    NotificationType Type,
    NotificationChannel Channel,
    string Subject,
    string Body,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? PlainTextBody = null,
    Dictionary<string, string>? Metadata = null,
    string? TemplateKey = null,
    Dictionary<string, object>? TemplateVariables = null,
    string? CampaignId = null);

public record CancelNotificationRequest(string? Reason = null);
