using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Api.Models.Requests;
using AsanRezerve.ServiceCatalog.Api.Models.Responses;
using AsanRezerve.ServiceCatalog.API.Models.Requests;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.CreateReview;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.EditReview;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.ManageReply;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.ReportReview;
using AsanRezerve.ServiceCatalog.Application.Commands.Review.CastReviewVote;
using AsanRezerve.ServiceCatalog.Application.Queries.Review.GetProviderReviews;
using AsanRezerve.ServiceCatalog.Application.Queries.Review.ManagedReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AsanRezerve.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Reviews API
/// Manages customer reviews and ratings for providers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        ISender mediator,
        ILogger<ReviewsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get reviews for a provider with pagination and filtering
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="request">Query parameters (pagination, filters, sorting)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated reviews with statistics</returns>
    /// <remarks>
    /// Returns all reviews for the specified provider with filtering and sorting options.
    ///
    /// Features:
    /// - Pagination support (1-100 items per page)
    /// - Filter by rating range (minRating, maxRating)
    /// - Filter by verified reviews only
    /// - Sort by date, rating, or helpful count
    /// - Includes review statistics (average rating, rating distribution, etc.)
    ///
    /// Sample request:
    ///
    ///     GET /api/v1/reviews/providers/123e4567-e89b-12d3-a456-426614174000?pageNumber=1&amp;pageSize=20&amp;verifiedOnly=true&amp;sortBy=date&amp;sortDescending=true
    ///
    /// Sample response includes:
    /// - Review statistics (total, average rating, rating distribution)
    /// - Paginated review items with customer comments and provider responses
    /// - Helpfulness metrics for each review
    ///
    /// </remarks>
    /// <response code="200">Returns the paginated reviews</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("providers/{providerId}")]
    [AllowAnonymous]
    [EnableRateLimiting("provider-reviews")]
    [ProducesResponseType(typeof(ProviderReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderReviews(
        [FromRoute] Guid providerId,
        [FromQuery] GetProviderReviewsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting reviews for provider {ProviderId}, Page: {Page}, Size: {Size}",
            providerId,
            request.PageNumber,
            request.PageSize);

        // Validate request
        if (request.MinRating.HasValue && request.MaxRating.HasValue &&
            request.MinRating > request.MaxRating)
        {
            return BadRequest(new ApiErrorResponse(
                "ERR_VALIDATION",
                "MinRating cannot be greater than MaxRating",
                "MinRating"));
        }

        var query = new GetProviderReviewsQuery(
            ProviderId: providerId,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            MinRating: request.MinRating,
            MaxRating: request.MaxRating,
            VerifiedOnly: request.VerifiedOnly,
            SortBy: request.SortBy,
            SortDescending: request.SortDescending,
            // Anonymous is fine; a signed-in reader is also told their own vote on each review.
            CallerId: CallerId());

        try
        {
            var viewModel = await _mediator.Send(query, cancellationToken);
            var response = MapToProviderReviewsResponse(viewModel);

            _logger.LogInformation(
                "Successfully retrieved {Count} reviews for provider {ProviderId} (Page {Page}/{TotalPages})",
                response.Reviews.Items.Count,
                providerId,
                response.Reviews.PageNumber,
                response.Reviews.TotalPages);

            return Ok(response);
        }
        catch (Core.Application.Exceptions.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Provider {ProviderId} not found", providerId);
            return NotFound(new ApiErrorResponse(
                "ERR_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for provider {ProviderId}", providerId);
            throw;
        }
    }

    /// <summary>
    /// Create a review for a completed booking
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="request">Review data (rating, comment)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created review</returns>
    /// <remarks>
    /// Creates a new review for a completed booking.
    ///
    /// Business rules:
    /// - Only the customer who made the booking can create a review
    /// - Booking must be in "Completed" status
    /// - Each booking can only have one review, and each customer one review per salon (409 naming «نظرهای من»)
    /// - Rating must be between 1.0 and 5.0 in 0.5 increments
    /// - Comment is optional but must be 10-2000 characters if provided
    /// - Reviews from actual bookings are automatically marked as verified
    ///
    /// Sample request:
    ///
    ///     POST /api/v1/reviews/bookings/123e4567-e89b-12d3-a456-426614174000
    ///     {
    ///       "cleanlinessRating": 5, "skillRating": 4.5, "punctualityRating": 4, "conductRating": 5,
    ///       "showName": true,
    ///       "comment": "عالی بود! خیلی راضی بودم از خدمات. حتما دوباره میام."
    ///     }
    ///
    /// Without "rating" the overall is the four aspects' average to the nearest half star (all four required);
    /// older apps still send "rating", kept as sent. "showName": false signs the public review «مشتری».
    ///
    /// </remarks>
    /// <response code="201">Review created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not the booking owner</response>
    /// <response code="404">Booking not found</response>
    /// <response code="409">Review already exists or booking not completed</response>
    [HttpPost("bookings/{bookingId}")]
    [Authorize]
    [EnableRateLimiting("create-review")]
    [ProducesResponseType(typeof(CreateReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReview(
        [FromRoute] Guid bookingId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get customer ID from authenticated user
        // See UsersController: production tokens carry the identity as nameidentifier.
        var customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
        {
            _logger.LogWarning("Invalid or missing customer ID in token");
            return Unauthorized(new ApiErrorResponse(
                "ERR_UNAUTHORIZED",
                "Invalid authentication token"));
        }

        _logger.LogInformation(
            "Creating review for booking {BookingId} by customer {CustomerId}",
            bookingId,
            customerId);

        // Validate rating increments
        if (request.Rating is { } stated && stated % 0.5m != 0)
        {
            return BadRequest(new ApiErrorResponse(
                "ERR_VALIDATION",
                "امتیاز کلی باید مضربی از نیم ستاره باشد (مثلاً ۳٫۵ یا ۴).",
                "Rating"));
        }

        var command = new CreateReviewCommand(
            BookingId: bookingId,
            CustomerId: customerId,
            Rating: request.Rating,
            Comment: request.Comment,
            Dimensions: new Domain.ValueObjects.ReviewDimensionRatings(
                request.CleanlinessRating, request.SkillRating, request.PunctualityRating, request.ConductRating),
            ShowName: request.ShowName);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            var response = MapToCreateReviewResponse(result);

            _logger.LogInformation(
                "Review {ReviewId} created successfully for booking {BookingId} with rating {Rating}★",
                result.ReviewId,
                bookingId,
                result.Rating);

            return CreatedAtAction(
                nameof(GetProviderReviews),
                new { providerId = result.ProviderId },
                response);
        }
        catch (Core.Application.Exceptions.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Booking {BookingId} not found", bookingId);
            return NotFound(new ApiErrorResponse(
                "ERR_NOT_FOUND",
                ex.Message));
        }
        catch (Core.Application.Exceptions.ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} not authorized for booking {BookingId}",
                customerId, bookingId);
            // With the reason: a bare Forbid() has no body, so the app could only say «ثبت نظر ناموفق بود».
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse("ERR_FORBIDDEN", ex.Message));
        }
        catch (Core.Application.Exceptions.ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict creating review for booking {BookingId}", bookingId);
            return Conflict(new ApiErrorResponse(
                "ERR_CONFLICT",
                ex.Message));
        }
        catch (Core.Domain.Exceptions.DomainValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating review for booking {BookingId}", bookingId);
            // The customer's words, not the wrapper («Validation failed for property …»), with the field they are about.
            var (field, messages) = ex.ValidationErrors.FirstOrDefault();
            return BadRequest(new ApiErrorResponse(
                "ERR_VALIDATION",
                messages?.FirstOrDefault() ?? ex.Message,
                field));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for booking {BookingId}", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Vote a published review helpful or not helpful — one vote per signed-in user.
    /// </summary>
    /// <remarks>
    /// **BREAKING (provider-reviews-and-ratings):** this used to be anonymous and a bare counter increment, so anyone
    /// could inflate a review without limit. It now needs a signed-in user and holds one vote per user per review:
    /// a first vote adds, repeating the vote you hold withdraws it, and the opposite vote moves it. The response
    /// reports the caller's vote after the request as `myVote` ("helpful" | "notHelpful" | null).
    ///
    ///     PUT /api/v1/reviews/{reviewId}/helpful
    ///     { "isHelpful": true }
    /// </remarks>
    /// <response code="200">Vote recorded, moved, or withdrawn</response>
    /// <response code="400">The review is not published</response>
    /// <response code="401">Not signed in</response>
    /// <response code="403">The review's own author</response>
    /// <response code="404">Review not found</response>
    [HttpPut("{reviewId}/helpful")]
    [Authorize]
    [EnableRateLimiting("mark-review-helpful")]
    [ProducesResponseType(typeof(MarkReviewHelpfulResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewHelpful(
        [FromRoute] Guid reviewId,
        [FromBody] MarkReviewHelpfulRequest request,
        CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } voterId)
            return Unauthorized(new ApiErrorResponse("ERR_UNAUTHORIZED", "Invalid authentication token"));

        var result = await _mediator.Send(
            new CastReviewVoteCommand(reviewId, voterId, request.IsHelpful), cancellationToken);

        return Ok(new MarkReviewHelpfulResponse
        {
            ReviewId = result.ReviewId,
            HelpfulCount = result.HelpfulCount,
            NotHelpfulCount = result.NotHelpfulCount,
            HelpfulnessRatio = result.HelpfulnessRatio,
            IsConsideredHelpful = result.IsConsideredHelpful,
            MyVote = VoteName(result.MyVote),
        });
    }

    /// <summary>The wire form of a user's vote.</summary>
    internal static string? VoteName(bool? vote) => vote switch
    {
        true => "helpful",
        false => "notHelpful",
        null => null,
    };

    /// <summary>
    /// The author edits their review.
    /// </summary>
    /// <remarks>
    /// Allowed within 7 days of writing it, while it is pending or published. A published review goes back to the
    /// moderation queue and leaves the provider's rating until it is approved again. A rejected or hidden review
    /// cannot be edited. Same body as creating a review.
    /// </remarks>
    /// <response code="200">Edited; now awaiting approval</response>
    /// <response code="400">Invalid ratings or comment, edit window closed, or the review is rejected/hidden</response>
    /// <response code="403">Not the review's author</response>
    /// <response code="404">Review not found</response>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [EnableRateLimiting("edit-review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditReview(
        [FromRoute] Guid reviewId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } editorId)
            return Unauthorized(new ApiErrorResponse("ERR_UNAUTHORIZED", "Invalid authentication token"));

        var result = await _mediator.Send(new EditReviewCommand(
            reviewId,
            editorId,
            request.Rating,
            request.Comment,
            new Domain.ValueObjects.ReviewDimensionRatings(
                request.CleanlinessRating, request.SkillRating, request.PunctualityRating, request.ConductRating),
            request.ShowName),
            cancellationToken);

        return Ok(new
        {
            result.ReviewId,
            ModerationStatus = result.ModerationStatus.ToString(),
            result.EditedAt,
            result.Rating,
            result.ShowName,
        });
    }

    /// <summary>The reviewed business replies. Owner or manager only; never an administrator. Goes to moderation.</summary>
    /// <response code="409">The review already has a reply — edit it instead</response>
    [HttpPost("{reviewId:guid}/reply")]
    [Authorize]
    [EnableRateLimiting("reply-review")]
    public Task<IActionResult> AddReply(Guid reviewId, [FromBody] ReplyRequest body, CancellationToken ct) =>
        ReplyAsync(reviewId, ReplyAction.Add, body.Text, ct);

    /// <summary>The business rewrites its reply. It goes back to moderation.</summary>
    [HttpPut("{reviewId:guid}/reply")]
    [Authorize]
    [EnableRateLimiting("reply-review")]
    public Task<IActionResult> EditReply(Guid reviewId, [FromBody] ReplyRequest body, CancellationToken ct) =>
        ReplyAsync(reviewId, ReplyAction.Edit, body.Text, ct);

    /// <summary>The business withdraws its reply.</summary>
    [HttpDelete("{reviewId:guid}/reply")]
    [Authorize]
    [EnableRateLimiting("reply-review")]
    public Task<IActionResult> RemoveReply(Guid reviewId, CancellationToken ct) =>
        ReplyAsync(reviewId, ReplyAction.Remove, null, ct);

    private async Task<IActionResult> ReplyAsync(Guid reviewId, ReplyAction action, string? text, CancellationToken ct)
    {
        if (CallerId() is not { } callerId)
            return Unauthorized(new ApiErrorResponse("ERR_UNAUTHORIZED", "Invalid authentication token"));

        var result = await _mediator.Send(new ManageReplyCommand(reviewId, action, text, $"Provider:{callerId}"), ct);
        return Ok(new
        {
            result.ReviewId,
            result.ProviderResponse,
            ReplyModerationStatus = result.ReplyModerationStatus?.ToString(),
        });
    }

    /// <summary>
    /// Report a published review, with a reason. Any signed-in user may — including the reviewed provider. The
    /// review stays public until an administrator acts.
    /// </summary>
    /// <response code="400">No reason, or the review is not public</response>
    /// <response code="409">You have already reported this review</response>
    [HttpPost("{reviewId:guid}/report")]
    [Authorize]
    [EnableRateLimiting("report-review")]
    public async Task<IActionResult> ReportReview(
        Guid reviewId, [FromBody] ReportReviewRequest body, CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } reporterId)
            return Unauthorized(new ApiErrorResponse("ERR_UNAUTHORIZED", "Invalid authentication token"));

        var result = await _mediator.Send(new ReportReviewCommand(reviewId, reporterId, body.Reason), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// The signed-in customer's own reviews, in every state — each with its moderation state, the administrator's
    /// reason where there is one, and whether it can still be edited.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ManagedReviewsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } customerId)
            return Unauthorized(new ApiErrorResponse("ERR_UNAUTHORIZED", "Invalid authentication token"));

        return Ok(await _mediator.Send(new GetMyReviewsQuery(customerId, pageNumber, pageSize), cancellationToken));
    }

    /// <summary>
    /// Every review of a business, in every state — its owner and managers only. The public listing stays
    /// published-only for everyone, the owner included; this is the separate surface where pending reviews live.
    /// </summary>
    [HttpGet("providers/{providerId:guid}/inbox")]
    [Authorize]
    [ProducesResponseType(typeof(ManagedReviewsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProviderReviewInbox(
        Guid providerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetProviderReviewInboxQuery(providerId, pageNumber, pageSize), cancellationToken));

    private static DimensionStatisticResponse Dimension(Domain.Policies.DimensionRating rating) =>
        new() { Average = rating.Average, Count = rating.Count };

    /// <summary>
    /// The signed-in user's id. Production tokens carry it as nameidentifier, not sub/userId — reading sub first
    /// is how every real user once got a 403 the test tokens never showed.
    /// </summary>
    private Guid? CallerId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("userId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    #region Mapping Methods

    private ProviderReviewsResponse MapToProviderReviewsResponse(GetProviderReviewsViewModel viewModel)
    {
        return new ProviderReviewsResponse
        {
            ProviderId = viewModel.ProviderId,
            Statistics = new ReviewStatisticsResponse
            {
                TotalReviews = viewModel.Statistics.TotalReviews,
                VerifiedReviews = viewModel.Statistics.VerifiedReviews,
                AverageRating = viewModel.Statistics.AverageRating,
                RatingDistribution = new RatingDistributionResponse
                {
                    FiveStarCount = viewModel.Statistics.RatingDistribution.FiveStarCount,
                    FourStarCount = viewModel.Statistics.RatingDistribution.FourStarCount,
                    ThreeStarCount = viewModel.Statistics.RatingDistribution.ThreeStarCount,
                    TwoStarCount = viewModel.Statistics.RatingDistribution.TwoStarCount,
                    OneStarCount = viewModel.Statistics.RatingDistribution.OneStarCount,
                    FiveStarPercentage = viewModel.Statistics.RatingDistribution.FiveStarPercentage,
                    FourStarPercentage = viewModel.Statistics.RatingDistribution.FourStarPercentage,
                    ThreeStarPercentage = viewModel.Statistics.RatingDistribution.ThreeStarPercentage,
                    TwoStarPercentage = viewModel.Statistics.RatingDistribution.TwoStarPercentage,
                    OneStarPercentage = viewModel.Statistics.RatingDistribution.OneStarPercentage
                },
                ReviewsWithComments = viewModel.Statistics.ReviewsWithComments,
                ReviewsWithProviderResponse = viewModel.Statistics.ReviewsWithProviderResponse,
                MostRecentReviewDate = viewModel.Statistics.MostRecentReviewDate,
                OldestReviewDate = viewModel.Statistics.OldestReviewDate,
                Cleanliness = Dimension(viewModel.Statistics.Cleanliness),
                Skill = Dimension(viewModel.Statistics.Skill),
                Punctuality = Dimension(viewModel.Statistics.Punctuality),
                Conduct = Dimension(viewModel.Statistics.Conduct)
            },
            Reviews = new PaginatedReviewsResponse
            {
                Items = viewModel.Reviews.Items.Select(MapToReviewResponse).ToList(),
                TotalCount = viewModel.Reviews.TotalCount,
                PageNumber = viewModel.Reviews.PageNumber,
                PageSize = viewModel.Reviews.PageSize,
                TotalPages = viewModel.Reviews.TotalPages,
                HasNextPage = viewModel.Reviews.HasNextPage,
                HasPreviousPage = viewModel.Reviews.HasPreviousPage
            }
        };
    }

    private ReviewResponse MapToReviewResponse(ReviewItemViewModel item)
    {
        return new ReviewResponse
        {
            ReviewId = item.ReviewId,
            ProviderId = item.ProviderId,
            CustomerId = item.CustomerId,
            CustomerName = item.CustomerName,
            BookingId = item.BookingId,
            Rating = item.Rating,
            Comment = item.Comment,
            IsVerified = item.IsVerified,
            ProviderResponse = item.ProviderResponse,
            ProviderResponseAt = item.ProviderResponseAt,
            HelpfulCount = item.HelpfulCount,
            NotHelpfulCount = item.NotHelpfulCount,
            HelpfulnessRatio = item.HelpfulnessRatio,
            IsConsideredHelpful = item.IsConsideredHelpful,
            CreatedAt = item.CreatedAt,
            AgeInDays = item.AgeInDays,
            IsRecent = item.IsRecent,
            CleanlinessRating = item.CleanlinessRating,
            SkillRating = item.SkillRating,
            PunctualityRating = item.PunctualityRating,
            ConductRating = item.ConductRating,
            MyVote = VoteName(item.MyVote)
        };
    }

    private CreateReviewResponse MapToCreateReviewResponse(CreateReviewResult result)
    {
        return new CreateReviewResponse
        {
            ReviewId = result.ReviewId,
            ProviderId = result.ProviderId,
            CustomerId = result.CustomerId,
            BookingId = result.BookingId,
            Rating = result.Rating,
            Comment = result.Comment,
            IsVerified = result.IsVerified,
            CreatedAt = result.CreatedAt,
            ModerationStatus = result.ModerationStatus.ToString(),
            CleanlinessRating = result.Dimensions.Cleanliness,
            SkillRating = result.Dimensions.Skill,
            PunctualityRating = result.Dimensions.Punctuality,
            ConductRating = result.Dimensions.Conduct,
            ShowName = result.ShowName
        };
    }


    #endregion
}

/// <summary>A provider's reply to a review: 1–1000 characters after trimming.</summary>
public sealed class ReplyRequest
{
    public string? Text { get; set; }
}

/// <summary>Why a review should not be public. Required, at most 500 characters.</summary>
public sealed class ReportReviewRequest
{
    public string? Reason { get; set; }
}
