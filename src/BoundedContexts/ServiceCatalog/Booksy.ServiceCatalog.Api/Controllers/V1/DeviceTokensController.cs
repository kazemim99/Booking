using Booksy.API.Extensions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

/// <summary>What the app sends when it has a push token.</summary>
/// <param name="Token">The gateway's registration token for this installation.</param>
/// <param name="Platform">Which app store this build came from.</param>
public sealed record RegisterDeviceTokenRequest(string Token, DevicePlatform Platform);

/// <summary>
/// The devices a person can be pushed to.
/// </summary>
/// <remarks>
/// Every action is scoped to the authenticated caller. A device is never addressed by user id from the
/// outside: taking one would let somebody register a token against another account, or silence theirs.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenRegistry _registry;
    private readonly ILogger<DeviceTokensController> _logger;

    public DeviceTokensController(IDeviceTokenRegistry registry, ILogger<DeviceTokensController> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Registers this device for push, or refreshes it if already known.
    /// </summary>
    /// <remarks>
    /// Safe to call on every app start: re-registering refreshes the existing row rather than adding a
    /// second one, which is what the app should do since a token can be rotated by the OS at any time.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("A device token is required.");

        await _registry.RegisterAsync(User.GetUserId(), request.Token, request.Platform, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Stops pushing to one device. Call this on sign-out.
    /// </summary>
    /// <remarks>
    /// Returns 204 whether or not the token was registered: telling an unauthenticated-ish caller which
    /// tokens exist would be a disclosure, and the outcome the caller wants — "this device receives nothing"
    /// — is true either way.
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("A device token is required.");

        var revoked = await _registry.RevokeAsync(User.GetUserId(), token, cancellationToken);

        if (!revoked)
            _logger.LogDebug("Revoke was a no-op: the token is unknown or already revoked");

        return NoContent();
    }
}
