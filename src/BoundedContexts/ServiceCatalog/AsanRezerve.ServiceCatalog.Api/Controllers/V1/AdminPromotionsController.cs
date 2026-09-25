using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Application.Promotions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsanRezerve.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Platform campaigns and oversight of every promotion (openspec/changes/add-discounts-and-campaigns).
/// </summary>
/// <remarks>
/// <c>AdminOnly</c>, never a raw role list — see ReviewModerationController and FOLLOW-UPS #46.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/promotions")]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")]
public class AdminPromotionsController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminPromotionsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Every promotion, newest first. <paramref name="owner"/>: Platform | Provider | all;
    /// <paramref name="status"/>: Active | Paused | Ended | all; <paramref name="search"/> matches the title or the code.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PromotionPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? owner,
        [FromQuery] Guid? providerId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new SearchPromotionsQuery(owner, providerId, status, search, page, pageSize), cancellationToken));

    /// <summary>One promotion with its usage and, for a campaign, the salons that joined it.</summary>
    [HttpGet("{promotionId:guid}")]
    [ProducesResponseType(typeof(PromotionDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Details(Guid promotionId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPromotionDetailsQuery(promotionId), cancellationToken));

    /// <summary>Publishes a platform campaign. Salons opt in; each funds the discount it honours.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] PromotionTermsInput request, CancellationToken cancellationToken)
    {
        var created = await _mediator.Send(new CreatePlatformCampaignCommand(request, CurrentUserId()), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Edits a platform campaign. A salon's own promotion can be paused or ended, not reworded.</summary>
    [HttpPut("{promotionId:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid promotionId, [FromBody] PromotionTermsInput request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new UpdatePlatformCampaignCommand(promotionId, request), cancellationToken));

    /// <summary>Pauses any promotion. A salon cannot resume what an administrator paused.</summary>
    [HttpPost("{promotionId:guid}/pause")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Pause(Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(promotionId, PromotionLifecycleAction.Pause, cancellationToken);

    [HttpPost("{promotionId:guid}/resume")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Resume(Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(promotionId, PromotionLifecycleAction.Resume, cancellationToken);

    [HttpPost("{promotionId:guid}/end")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> End(Guid promotionId, CancellationToken cancellationToken) =>
        ChangeStatus(promotionId, PromotionLifecycleAction.End, cancellationToken);

    private async Task<IActionResult> ChangeStatus(Guid promotionId, PromotionLifecycleAction action, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new AdminChangePromotionStatusCommand(promotionId, action), cancellationToken));

    private Guid CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
