using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using MediatR;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.ManageReply;

public enum ReplyAction
{
    Add,
    Edit,
    Remove,
}

/// <summary>
/// The reviewed business adds, rewrites or withdraws its one reply to a review.
/// </summary>
/// <remarks>
/// <para><b>Who may.</b> Only someone who can act for the reviewed salon on profile-level matters — its owner or
/// a manager (<see cref="OrganizationPermission.ManageOrganization"/>), the same people who may change the salon's
/// public profile. A reply is published speech attributed to the business, so plain staff cannot write it, and
/// administrators cannot either: unlike <c>BookingsController.CanManageProvider</c> there is deliberately no
/// admin shortcut here (design D11).</para>
///
/// <para>Every add or edit goes back to moderation; the review it answers is untouched.</para>
/// </remarks>
public sealed record ManageReplyCommand(Guid ReviewId, ReplyAction Action, string? Text, string ActedBy)
    : ICommand<ManageReplyResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record ManageReplyResult(Guid ReviewId, string? ProviderResponse, ReviewModerationStatus? ReplyModerationStatus);

public sealed class ManageReplyCommandHandler : ICommandHandler<ManageReplyCommand, ManageReplyResult>
{
    private readonly IReviewWriteRepository _reviews;
    private readonly ISender _sender;

    public ManageReplyCommandHandler(IReviewWriteRepository reviews, ISender sender)
    {
        _reviews = reviews;
        _sender = sender;
    }

    public async Task<ManageReplyResult> Handle(ManageReplyCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException("این نظر پیدا نشد.");

        var mayActForSalon = await _sender.Send(
            new CanManageOrganizationQuery(review.ProviderId.Value, OrganizationPermission.ManageOrganization),
            cancellationToken);
        if (!mayActForSalon)
            throw new ForbiddenException("فقط خود سالن می‌تواند به این نظر پاسخ دهد.");

        switch (request.Action)
        {
            case ReplyAction.Add:
                if (review.ProviderResponse is not null)
                    throw new ConflictException("این نظر پاسخ دارد؛ همان پاسخ را ویرایش کنید.");
                review.AddProviderResponse(request.Text ?? string.Empty, request.ActedBy);
                break;
            case ReplyAction.Edit:
                review.UpdateProviderResponse(request.Text ?? string.Empty, request.ActedBy);
                break;
            case ReplyAction.Remove:
                review.RemoveProviderResponse(request.ActedBy);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unknown reply action");
        }

        await _reviews.UpdateAsync(review, cancellationToken);
        return new ManageReplyResult(review.Id, review.ProviderResponse, review.ReplyModerationStatus);
    }
}
