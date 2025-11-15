using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Review.GetProviderReviews;

/// <summary>
/// Handler for retrieving paginated provider reviews
/// </summary>
public sealed class GetProviderReviewsQueryHandler
    : IQueryHandler<GetProviderReviewsQuery, GetProviderReviewsViewModel>
{
    private readonly IReviewReadRepository _reviewRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly ILogger<GetProviderReviewsQueryHandler> _logger;

    public GetProviderReviewsQueryHandler(
        IReviewReadRepository reviewRepository,
        IProviderReadRepository providerRepository,
        ILogger<GetProviderReviewsQueryHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _providerRepository = providerRepository;
        _logger = logger;
    }

    public async Task<GetProviderReviewsViewModel> Handle(
        GetProviderReviewsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting reviews for Provider {ProviderId} (Page {Page}, Size {Size})",
            request.ProviderId,
            request.PageNumber,
            request.PageSize);

        var providerId = ProviderId.From(request.ProviderId);

        // Verify provider exists
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Get review statistics
        var statistics = await _reviewRepository.GetReviewStatisticsAsync(
            providerId,
            cancellationToken);

        // Get paginated reviews
        var paginatedReviews = await _reviewRepository.GetByProviderIdAsync(
            providerId,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            minRating: request.MinRating,
            maxRating: request.MaxRating,
            verifiedOnly: request.VerifiedOnly,
            sortBy: request.SortBy,
            sortDescending: request.SortDescending,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} reviews for Provider {ProviderId} (Page {Page}/{TotalPages})",
            paginatedReviews.Reviews.Count,
            request.ProviderId,
            paginatedReviews.PageNumber,
            paginatedReviews.TotalPages);

        return MapToViewModel(request.ProviderId, statistics, paginatedReviews);
    }

    private GetProviderReviewsViewModel MapToViewModel(
        Guid providerId,
        Domain.Repositories.ReviewStatistics statistics,
        Domain.Repositories.PaginatedReviews paginatedReviews)
    {
        // Calculate percentages for rating distribution
        var totalReviews = statistics.TotalReviews;
        var fiveStarPct = totalReviews > 0 ? (decimal)statistics.FiveStarCount / totalReviews * 100 : 0;
        var fourStarPct = totalReviews > 0 ? (decimal)statistics.FourStarCount / totalReviews * 100 : 0;
        var threeStarPct = totalReviews > 0 ? (decimal)statistics.ThreeStarCount / totalReviews * 100 : 0;
        var twoStarPct = totalReviews > 0 ? (decimal)statistics.TwoStarCount / totalReviews * 100 : 0;
        var oneStarPct = totalReviews > 0 ? (decimal)statistics.OneStarCount / totalReviews * 100 : 0;

        var statisticsViewModel = new ReviewStatisticsViewModel(
            TotalReviews: statistics.TotalReviews,
            VerifiedReviews: statistics.VerifiedReviews,
            AverageRating: statistics.AverageRating,
            RatingDistribution: new RatingDistributionViewModel(
                FiveStarCount: statistics.FiveStarCount,
                FourStarCount: statistics.FourStarCount,
                ThreeStarCount: statistics.ThreeStarCount,
                TwoStarCount: statistics.TwoStarCount,
                OneStarCount: statistics.OneStarCount,
                FiveStarPercentage: Math.Round(fiveStarPct, 1),
                FourStarPercentage: Math.Round(fourStarPct, 1),
                ThreeStarPercentage: Math.Round(threeStarPct, 1),
                TwoStarPercentage: Math.Round(twoStarPct, 1),
                OneStarPercentage: Math.Round(oneStarPct, 1)),
            ReviewsWithComments: statistics.ReviewsWithComments,
            ReviewsWithProviderResponse: statistics.ReviewsWithProviderResponse,
            MostRecentReviewDate: statistics.MostRecentReviewDate,
            OldestReviewDate: statistics.OldestReviewDate);

        var reviewItems = paginatedReviews.Reviews.Select(r => new ReviewItemViewModel(
            ReviewId: r.Id,
            ProviderId: r.ProviderId.Value,
            CustomerId: r.CustomerId.Value,
            CustomerName: GetCustomerDisplayName(r.CustomerId.Value), // Placeholder
            BookingId: r.BookingId,
            Rating: r.RatingValue,
            Comment: r.Comment,
            IsVerified: r.IsVerified,
            ProviderResponse: r.ProviderResponse,
            ProviderResponseAt: r.ProviderResponseAt,
            HelpfulCount: r.HelpfulCount,
            NotHelpfulCount: r.NotHelpfulCount,
            HelpfulnessRatio: r.GetHelpfulnessRatio(),
            IsConsideredHelpful: r.IsConsideredHelpful(),
            CreatedAt: r.CreatedAt,
            AgeInDays: r.GetAgeInDays(),
            IsRecent: r.IsRecent())).ToList();

        var reviewsViewModel = new PaginatedReviewsViewModel(
            Items: reviewItems,
            TotalCount: paginatedReviews.TotalCount,
            PageNumber: paginatedReviews.PageNumber,
            PageSize: paginatedReviews.PageSize,
            TotalPages: paginatedReviews.TotalPages,
            HasNextPage: paginatedReviews.PageNumber < paginatedReviews.TotalPages,
            HasPreviousPage: paginatedReviews.PageNumber > 1);

        return new GetProviderReviewsViewModel(
            ProviderId: providerId,
            Statistics: statisticsViewModel,
            Reviews: reviewsViewModel);
    }

    /// <summary>
    /// Gets customer display name (placeholder - in production would call UserManagement API)
    /// For now, returns anonymous customer identifier
    /// </summary>
    private string GetCustomerDisplayName(Guid customerId)
    {
        // TODO: Call UserManagement API to get actual customer name
        // For now, return first 8 chars of customer ID as identifier
        var shortId = customerId.ToString().Substring(0, 8);
        return $"Customer {shortId}";
    }
}
