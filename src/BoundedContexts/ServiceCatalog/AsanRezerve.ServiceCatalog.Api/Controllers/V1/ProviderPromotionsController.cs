using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Application.Promotions;
using AsanRezerve.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AsanRezerve.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// A salon's discounts and its participation in platform campaigns (openspec/changes/add-discounts-and-campaigns).
/// Pricing is running the salon, not working in it: admin, the owner, or a member allowed to manage the
/// organization (design D11). The public offers list is the only anonymous route.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/providers/{providerId:guid}")]
[Produces("application/json")]
public class ProviderPromotionsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProviderPromotionsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>The salon's own promotions, newest first, with their state and usage.</summary>
    [HttpGet("promotions")]
    [ProducesResponseType(typeof(IReadOnlyList<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid providerId, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new GetProviderPromotionsQuery(providerId), cancellationToken));
    }

    [HttpPost("promotions")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid providerId, [FromBody] PromotionTermsInput request, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        var created = await _mediator.Send(
            new CreateProviderPromotionCommand(providerId, request, CurrentUserId()), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Replaces the terms. Bookings already made keep their discount; a used code cannot change.</summary>
    [HttpPut("promotions/{promotionId:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid providerId, Guid promotionId, [FromBody] PromotionTermsInput request, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new UpdateProviderPromotionCommand(providerId, promotionId, request), cancellationToken));
    }

    [HttpPost("promotions/{promotionId:guid}/pause")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Pause(Guid providerId, Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(providerId, promotionId, PromotionLifecycleAction.Pause, cancellationToken);

    [HttpPost("promotions/{promotionId:guid}/resume")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Resume(Guid providerId, Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(providerId, promotionId, PromotionLifecycleAction.Resume, cancellationToken);

    /// <summary>Ends the promotion for good. Its history stays.</summary>
    [HttpPost("promotions/{promotionId:guid}/end")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> End(Guid providerId, Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(providerId, promotionId, PromotionLifecycleAction.End, cancellationToken);

    /// <summary>Platform campaigns the salon can join, and whether it has.</summary>
    [HttpGet("campaigns")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignForProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Campaigns(Guid providerId, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new GetCampaignsForProviderQuery(providerId), cancellationToken));
    }

    /// <summary>Joins a campaign: its discount then applies to new bookings here, funded by the salon.</summary>
    [HttpPost("campaigns/{campaignId:guid}/enrollment")]
    [ProducesResponseType(typeof(CampaignForProviderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(Guid providerId, Guid campaignId, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new JoinCampaignCommand(providerId, campaignId, CurrentUserId()), cancellationToken));
    }

    /// <summary>Leaves a campaign. Bookings already made keep their discount.</summary>
    [HttpDelete("campaigns/{campaignId:guid}/enrollment")]
    [ProducesResponseType(typeof(CampaignForProviderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Leave(Guid providerId, Guid campaignId, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new LeaveCampaignCommand(providerId, campaignId, CurrentUserId()), cancellationToken));
    }

    /// <summary>
    /// The salon's automatic offers in force now, for its public page. Never lists codes.
    /// </summary>
    [HttpGet("offers")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicOfferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Offers(Guid providerId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetProviderOffersQuery(providerId), cancellationToken));

    private async Task<IActionResult> ChangeStatus(
        Guid providerId, Guid promotionId, PromotionLifecycleAction action, CancellationToken cancellationToken)
    {
        if (!await CanManagePricing(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new ChangeProviderPromotionStatusCommand(providerId, promotionId, action), cancellationToken));
    }

    private Guid CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<bool> CanManagePricing(Guid providerId)
    {
        if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            return false;

        if (User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("SysAdmin"))
            return true;

        if (User.FindFirst("providerId")?.Value == providerId.ToString())
            return true;

        // Discounts are pricing — running the salon — so ManageOrganization (owners and managers), not ManageBookings.
        return await _mediator.Send(new CanManageOrganizationQuery(providerId, OrganizationPermission.ManageOrganization));
    }
}
