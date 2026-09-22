using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Review.GetModerationQueue;

/// <summary>The administrator's moderation queue, one filter at a time.</summary>
public sealed record GetModerationQueueQuery(
    ReviewModerationFilter Filter,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<ModerationQueueViewModel>;

public sealed record ModerationQueueViewModel(
    IReadOnlyList<ModerationQueueItem> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

/// <summary>Everything a moderator needs to decide, without opening anything else.</summary>
public sealed record ModerationQueueItem(
    Guid ReviewId,
    Guid ProviderId,
    Guid CustomerId,
    Guid BookingId,
    decimal Rating,
    decimal? CleanlinessRating,
    decimal? SkillRating,
    decimal? PunctualityRating,
    decimal? ConductRating,
    string? Comment,
    string ModerationStatus,
    string? ModerationReason,
    bool ReviewPending,
    bool WasPublishedBefore,
    string? ProviderResponse,
    string? ReplyModerationStatus,
    bool ReplyPending,
    DateTime CreatedAt,
    DateTime? EditedAt,
    int ReportCount,
    IReadOnlyList<ModerationQueueReport> Reports);

/// <summary>One report against a review: why, and by whom.</summary>
public sealed record ModerationQueueReport(string Reason, Guid ReportedByUserId, DateTime CreatedAt);

public sealed class GetModerationQueueQueryHandler : IQueryHandler<GetModerationQueueQuery, ModerationQueueViewModel>
{
    private readonly IReviewReadRepository _reviews;

    public GetModerationQueueQueryHandler(IReviewReadRepository reviews) => _reviews = reviews;

    public async Task<ModerationQueueViewModel> Handle(GetModerationQueueQuery request, CancellationToken cancellationToken)
    {
        var page = await _reviews.GetModerationQueueAsync(
            request.Filter, request.PageNumber, request.PageSize, cancellationToken);

        var reports = (await _reviews.GetReportsAsync(page.Reviews.Select(r => r.Id).ToList(), cancellationToken))
            .GroupBy(r => r.ReviewId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ModerationQueueReport>)g
                    .Select(r => new ModerationQueueReport(r.Reason, r.ReportedByUserId.Value, r.CreatedAt))
                    .ToList());

        var items = page.Reviews.Select(r => new ModerationQueueItem(
            r.Id,
            r.ProviderId.Value,
            r.CustomerId.Value,
            r.BookingId,
            r.RatingValue,
            r.CleanlinessRating,
            r.SkillRating,
            r.PunctualityRating,
            r.ConductRating,
            r.Comment,
            r.ModerationStatus.ToString(),
            r.ModerationReason,
            ReviewPending: r.ModerationStatus == Domain.Enums.ReviewModerationStatus.Pending,
            WasPublishedBefore: r.FirstPublishedAt is not null,
            r.ProviderResponse,
            r.ReplyModerationStatus?.ToString(),
            ReplyPending: r.ReplyModerationStatus == Domain.Enums.ReviewModerationStatus.Pending,
            r.CreatedAt,
            r.EditedAt,
            reports.TryGetValue(r.Id, out var filed) ? filed.Count : 0,
            reports.TryGetValue(r.Id, out var list) ? list : Array.Empty<ModerationQueueReport>())).ToList();

        return new ModerationQueueViewModel(items, page.TotalCount, page.PageNumber, page.PageSize, page.TotalPages);
    }
}
