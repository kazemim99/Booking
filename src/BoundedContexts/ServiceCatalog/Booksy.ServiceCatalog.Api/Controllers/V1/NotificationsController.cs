using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Commands.Notifications.CancelNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.ResendNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory;
using Booksy.ServiceCatalog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

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
            request.RecipientPhone,
            request.BookingId,
            request.ProviderId,
            request.PaymentId,
            request.Metadata,
            request.TemplateKey,
            request.TemplateVariables);

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
            request.RecipientPhone,
            request.BookingId,
            request.ProviderId,
            request.PaymentId,
            request.Metadata,
            request.TemplateKey,
            request.TemplateVariables);

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
            request.PlainTextBody,
            request.Metadata,
            request.TemplateKey,
            request.TemplateVariables,
            request.CampaignId);

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
        var query = new GetDeliveryStatusQuery(notificationId);
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
