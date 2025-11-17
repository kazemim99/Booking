using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Application.Commands.Review.CreateReview;
using Booksy.ServiceCatalog.Application.Commands.Review.MarkReviewHelpful;
using Booksy.ServiceCatalog.Application.Queries.Review.GetProviderReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

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
            SortDescending: request.SortDescending);

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
    /// - Each booking can only have one review
    /// - Rating must be between 1.0 and 5.0 in 0.5 increments
    /// - Comment is optional but must be 10-2000 characters if provided
    /// - Reviews from actual bookings are automatically marked as verified
    ///
    /// Sample request:
    ///
    ///     POST /api/v1/reviews/bookings/123e4567-e89b-12d3-a456-426614174000
    ///     {
    ///       "rating": 4.5,
    ///       "comment": "عالی بود! خیلی راضی بودم از خدمات. حتما دوباره میام."
    ///     }
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
        var customerIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
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
        if (request.Rating % 0.5m != 0)
        {
            return BadRequest(new ApiErrorResponse(
                "ERR_VALIDATION",
                "Rating must be in 0.5 increments (e.g., 3.5, 4.0, 4.5)",
                "Rating"));
        }

        var command = new CreateReviewCommand(
            BookingId: bookingId,
            CustomerId: customerId,
            Rating: request.Rating,
            Comment: request.Comment);

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
            return Forbid();
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
            return BadRequest(new ApiErrorResponse(
                "ERR_VALIDATION",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for booking {BookingId}", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Mark a review as helpful or not helpful
    /// </summary>
    /// <param name="reviewId">Review ID</param>
    /// <param name="request">Helpfulness indicator</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated helpfulness metrics</returns>
    /// <remarks>
    /// Allows users to vote on whether a review is helpful or not.
    ///
    /// Features:
    /// - Increments either HelpfulCount or NotHelpfulCount
    /// - Returns updated helpfulness ratio
    /// - No authentication required (allows anonymous feedback)
    /// - Rate limited to prevent abuse
    ///
    /// Sample request:
    ///
    ///     PUT /api/v1/reviews/123e4567-e89b-12d3-a456-426614174000/helpful
    ///     {
    ///       "isHelpful": true
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Helpfulness recorded successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Review not found</response>
    [HttpPut("{reviewId}/helpful")]
    [AllowAnonymous]
    [EnableRateLimiting("mark-review-helpful")]
    [ProducesResponseType(typeof(MarkReviewHelpfulResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewHelpful(
        [FromRoute] Guid reviewId,
        [FromBody] MarkReviewHelpfulRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Marking review {ReviewId} as {Helpful}",
            reviewId,
            request.IsHelpful ? "helpful" : "not helpful");

        var command = new MarkReviewHelpfulCommand(
            ReviewId: reviewId,
            IsHelpful: request.IsHelpful);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            var response = MapToMarkReviewHelpfulResponse(result);

            _logger.LogInformation(
                "Review {ReviewId} marked as {Helpful}. New counts: {HelpfulCount} helpful, {NotHelpfulCount} not helpful",
                reviewId,
                request.IsHelpful ? "helpful" : "not helpful",
                result.HelpfulCount,
                result.NotHelpfulCount);

            return Ok(response);
        }
        catch (Core.Application.Exceptions.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Review {ReviewId} not found", reviewId);
            return NotFound(new ApiErrorResponse(
                "ERR_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking review {ReviewId} as helpful", reviewId);
            throw;
        }
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
                OldestReviewDate = viewModel.Statistics.OldestReviewDate
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
            IsRecent = item.IsRecent
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
            CreatedAt = result.CreatedAt
        };
    }

    private MarkReviewHelpfulResponse MapToMarkReviewHelpfulResponse(MarkReviewHelpfulResult result)
    {
        return new MarkReviewHelpfulResponse
        {
            ReviewId = result.ReviewId,
            HelpfulCount = result.HelpfulCount,
            NotHelpfulCount = result.NotHelpfulCount,
            HelpfulnessRatio = result.HelpfulnessRatio,
            IsConsideredHelpful = result.IsConsideredHelpful
        };
    }

    #endregion
}
