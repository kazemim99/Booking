using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Commands.Notifications.UpdatePreferences;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetUserPreferences;
using Booksy.ServiceCatalog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages user notification preferences
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications/[controller]")]
[Produces("application/json")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(
        ISender mediator,
        ILogger<PreferencesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get notification preferences for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserPreferencesViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = new GetUserPreferencesQuery(Guid.Parse(userId));
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            // Return default preferences if none exist
            return Ok(new UserPreferencesViewModel(
                Guid.Parse(userId),
                new List<string> { "Email", "SMS", "InApp" },
                new List<string> { "All" },
                null,
                null,
                "en",
                false,
                50,
                DateTime.UtcNow,
                DateTime.UtcNow));
        }

        return Ok(result);
    }

    /// <summary>
    /// Update notification preferences for the current user
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(
            Guid.Parse(userId),
            request.EnabledChannels,
            request.EnabledTypes,
            request.QuietHoursStart,
            request.QuietHoursEnd,
            request.PreferredLanguage,
            request.MarketingOptIn,
            request.MaxNotificationsPerDay,
            request.ResetToDefaults,
            request.SetMinimal);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Enable specific notification channels
    /// </summary>
    [HttpPost("channels/enable")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableChannels(
        [FromBody] EnableChannelsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(
           Guid.Parse(userId),
            EnabledChannels: request.Channels);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Enable specific notification types
    /// </summary>
    [HttpPost("types/enable")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableTypes(
        [FromBody] EnableTypesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(
           Guid.Parse(userId),
            EnabledTypes: request.Types);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Set quiet hours for notifications
    /// </summary>
    [HttpPut("quiet-hours")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetQuietHours(
        [FromBody] QuietHoursRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(
           Guid.Parse(userId),
            QuietHoursStart: request.Start,
            QuietHoursEnd: request.End);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update marketing opt-in status
    /// </summary>
    [HttpPut("marketing")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMarketingOptIn(
        [FromBody] MarketingOptInRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(
           Guid.Parse(userId),
            MarketingOptIn: request.OptIn);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reset preferences to defaults
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetToDefaults(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(userId, ResetToDefaults: true);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Set minimal notifications (only essential)
    /// </summary>
    [HttpPost("minimal")]
    [ProducesResponseType(typeof(UpdatePreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetMinimal(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdatePreferencesCommand(userId, SetMinimal: true);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record UpdatePreferencesRequest(
    NotificationChannel? EnabledChannels = null,
    NotificationType? EnabledTypes = null,
    TimeOnly? QuietHoursStart = null,
    TimeOnly? QuietHoursEnd = null,
    string? PreferredLanguage = null,
    bool? MarketingOptIn = null,
    int? MaxNotificationsPerDay = null,
    bool ResetToDefaults = false,
    bool SetMinimal = false);

public record EnableChannelsRequest(NotificationChannel Channels);

public record EnableTypesRequest(NotificationType Types);

public record QuietHoursRequest(TimeOnly? Start, TimeOnly? End);

public record MarketingOptInRequest(bool OptIn);
