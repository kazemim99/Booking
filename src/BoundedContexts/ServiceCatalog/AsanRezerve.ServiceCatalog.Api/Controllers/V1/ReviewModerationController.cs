using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.ModerateReview;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.RecomputeProviderRatings;
using AsanRezerve.ServiceCatalog.Application.Queries.Review.GetModerationQueue;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AsanRezerve.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Administrator-only review operations: moderation, and rating maintenance.
/// </summary>
/// <remarks>
/// <c>AdminOnly</c>, never a raw <c>Roles = "..."</c> list: the policy accepts every spelling a production
/// administrator may carry (Admin / Administrator / SysAdmin). A narrower list caused the 2026-09-19 incident in
/// which the real administrator got 403 on every admin page; see PolicyAuthorizationExtensions and FOLLOW-UPS #46.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/reviews")]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")]
public class ReviewModerationController : ControllerBase
{
    private readonly ISender _mediator;

    public ReviewModerationController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// The moderation queue. <c>pending</c> (default): every review or provider reply awaiting a decision, oldest
    /// first. <c>hidden</c>: reviews taken down, with the reason. <c>reported</c>: published reviews someone
    /// reported.
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(ModerationQueueViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Queue(
        [FromQuery] string filter = "pending",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ReviewModerationFilter>(filter, ignoreCase: true, out var parsed))
            return BadRequest(new { error = "filter must be one of: pending, hidden, reported" });

        return Ok(await _mediator.Send(new GetModerationQueueQuery(parsed, pageNumber, pageSize), cancellationToken));
    }

    /// <summary>Approve a pending review: it becomes public and counts toward the provider's rating.</summary>
    [HttpPost("{reviewId:guid}/approve")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> Approve(Guid reviewId, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.Approve, null, ct);

    /// <summary>Reject a pending review, with a reason. Permanent.</summary>
    [HttpPost("{reviewId:guid}/reject")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> Reject(Guid reviewId, [FromBody] ModerationReasonRequest body, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.Reject, body.Reason, ct);

    /// <summary>Take a published review down, with a reason. Reversible with restore.</summary>
    [HttpPost("{reviewId:guid}/hide")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> Hide(Guid reviewId, [FromBody] ModerationReasonRequest body, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.Hide, body.Reason, ct);

    /// <summary>Bring a hidden review back. A rejected review cannot be restored.</summary>
    [HttpPost("{reviewId:guid}/restore")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> Restore(Guid reviewId, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.Restore, null, ct);

    /// <summary>Approve the provider's pending reply.</summary>
    [HttpPost("{reviewId:guid}/reply/approve")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> ApproveReply(Guid reviewId, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.ApproveReply, null, ct);

    /// <summary>Reject the provider's pending reply, with a reason. The review itself is unaffected.</summary>
    [HttpPost("{reviewId:guid}/reply/reject")]
    [EnableRateLimiting("moderate-review")]
    public Task<IActionResult> RejectReply(Guid reviewId, [FromBody] ModerationReasonRequest body, CancellationToken ct) =>
        ModerateAsync(reviewId, ReviewModerationAction.RejectReply, body.Reason, ct);

    private async Task<IActionResult> ModerateAsync(
        Guid reviewId, ReviewModerationAction action, string? reason, CancellationToken ct)
    {
        // Production tokens carry the identity as nameidentifier, not sub/userId.
        var moderator = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("sub")?.Value
                        ?? "admin";
        var result = await _mediator.Send(
            new ModerateReviewCommand(reviewId, action, $"Admin:{moderator}", reason), ct);
        return Ok(new
        {
            result.ReviewId,
            ModerationStatus = result.ModerationStatus.ToString(),
            ReplyModerationStatus = result.ReplyModerationStatus?.ToString(),
        });
    }

    /// <summary>
    /// Recompute every provider's rating from their published reviews.
    /// </summary>
    /// <remarks>
    /// Run once after deploying the review-moderation migration: before it, nothing wrote a provider's rating,
    /// so every provider reads 0 however many reviews it has. Re-runnable — a second run changes nothing.
    /// </remarks>
    [HttpPost("recompute-ratings")]
    [ProducesResponseType(typeof(RecomputeProviderRatingsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecomputeRatings(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RecomputeProviderRatingsCommand(), cancellationToken);
        return Ok(result);
    }
}

/// <summary>Why an administrator rejected or hid something. Required.</summary>
public sealed class ModerationReasonRequest
{
    public string? Reason { get; set; }
}
