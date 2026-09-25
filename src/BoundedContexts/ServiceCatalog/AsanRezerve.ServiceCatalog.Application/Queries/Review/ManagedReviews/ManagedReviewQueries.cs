using AsanRezerve.ServiceCatalog.Application.Abstractions;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using MediatR;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Review.ManagedReviews;

/// <summary>
/// A review as someone responsible for it sees it — its author, or the business it is about — in any moderation
/// state, with the state, the administrator's reason, and the reply's own state.
/// </summary>
public sealed record ManagedReviewItem(
    Guid ReviewId,
    Guid ProviderId,
    Guid BookingId,
    decimal Rating,
    decimal? CleanlinessRating,
    decimal? SkillRating,
    decimal? PunctualityRating,
    decimal? ConductRating,
    string? Comment,
    string ModerationStatus,
    string? ModerationReason,
    string? ProviderResponse,
    string? ReplyModerationStatus,
    string? ReplyModerationReason,
    int HelpfulCount,
    int NotHelpfulCount,
    DateTime CreatedAt,
    DateTime? EditedAt,
    bool CanEdit,
    string? ProviderName = null,
    string? ProviderLogoUrl = null,
    string? ServiceName = null);

/// <param name="AwaitingReplyCount">Business inbox only: published reviews still waiting on the business's reply.</param>
public sealed record ManagedReviewsViewModel(
    IReadOnlyList<ManagedReviewItem> Items, int TotalCount, int? AwaitingReplyCount = null);

internal static class ManagedReviewMapping
{
    public static ManagedReviewItem ToItem(Domain.Aggregates.Review r, DateTime utcNow) => new(
        r.Id,
        r.ProviderId.Value,
        r.BookingId,
        r.RatingValue,
        r.CleanlinessRating,
        r.SkillRating,
        r.PunctualityRating,
        r.ConductRating,
        r.Comment,
        r.ModerationStatus.ToString(),
        r.ModerationReason,
        r.ProviderResponse,
        r.ReplyModerationStatus?.ToString(),
        r.ReplyModerationReason,
        r.HelpfulCount,
        r.NotHelpfulCount,
        r.CreatedAt,
        r.EditedAt,
        // What the author may do right now — the same two rules EditByAuthor enforces.
        CanEdit: r.ModerationStatus is ReviewModerationStatus.Pending or ReviewModerationStatus.Published
                 && ReviewEditPolicy.IsInsideWindow(r.CreatedAt, utcNow));
}

/// <summary>The signed-in customer's own reviews, every state, newest first.</summary>
public sealed record GetMyReviewsQuery(Guid CustomerId, int PageNumber = 1, int PageSize = 20)
    : IQuery<ManagedReviewsViewModel>;

public sealed class GetMyReviewsQueryHandler : IQueryHandler<GetMyReviewsQuery, ManagedReviewsViewModel>
{
    private readonly IReviewReadRepository _reviews;
    private readonly IUrlService _urls;

    public GetMyReviewsQueryHandler(IReviewReadRepository reviews, IUrlService urls)
    {
        _reviews = reviews;
        _urls = urls;
    }

    public async Task<ManagedReviewsViewModel> Handle(GetMyReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetByCustomerIdAsync(
            UserId.From(request.CustomerId), request.PageNumber, request.PageSize, cancellationToken);
        // The author's list names what they reviewed: the salon, its picture, and the service.
        var contexts = await _reviews.GetReviewContextsAsync(reviews, cancellationToken);
        var now = DateTime.UtcNow;
        return new ManagedReviewsViewModel(
            reviews.Select(r =>
            {
                var item = ManagedReviewMapping.ToItem(r, now);
                return contexts.TryGetValue(r.Id, out var c)
                    ? item with { ProviderName = c.ProviderName, ProviderLogoUrl = _urls.AbsoluteOrNull(c.ProviderLogoUrl), ServiceName = c.ServiceName }
                    : item;
            }).ToList(),
            reviews.Count);
    }
}

/// <summary>
/// Every review of a business, every state — its owner and managers only. Distinct from the public listing, which
/// returns published reviews to everyone, the owner included.
/// </summary>
public sealed record GetProviderReviewInboxQuery(Guid ProviderId, int PageNumber = 1, int PageSize = 20)
    : IQuery<ManagedReviewsViewModel>;

public sealed class GetProviderReviewInboxQueryHandler
    : IQueryHandler<GetProviderReviewInboxQuery, ManagedReviewsViewModel>
{
    private readonly IReviewReadRepository _reviews;
    private readonly ISender _sender;

    public GetProviderReviewInboxQueryHandler(IReviewReadRepository reviews, ISender sender)
    {
        _reviews = reviews;
        _sender = sender;
    }

    public async Task<ManagedReviewsViewModel> Handle(GetProviderReviewInboxQuery request, CancellationToken cancellationToken)
    {
        // The people who may reply are the people who see what they would reply to — including reviews still
        // pending, which may yet be rejected for their content.
        var mayActForSalon = await _sender.Send(
            new CanManageOrganizationQuery(request.ProviderId, OrganizationPermission.ManageOrganization),
            cancellationToken);
        if (!mayActForSalon)
            throw new ForbiddenException("Only the business can open its review inbox");

        var page = await _reviews.GetByProviderIdAsync(
            ProviderId.From(request.ProviderId),
            request.PageNumber,
            request.PageSize,
            cancellationToken: cancellationToken,
            publishedOnly: false);

        var awaitingReply = await _reviews.CountAwaitingReplyAsync(ProviderId.From(request.ProviderId), cancellationToken);

        var now = DateTime.UtcNow;
        return new ManagedReviewsViewModel(
            page.Reviews.Select(r => ManagedReviewMapping.ToItem(r, now)).ToList(), page.TotalCount, awaitingReply);
    }
}
