using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Commands.Review.ReportReview;

/// <summary>
/// A signed-in user — including the reviewed provider — reports a published review, with a reason.
/// </summary>
/// <remarks>
/// A report never removes anything: the review stays public until an administrator hides it. One report per user
/// per review — checked here for a clear 409, and held under concurrency by the unique index.
/// </remarks>
public sealed record ReportReviewCommand(Guid ReviewId, Guid ReporterId, string? Reason) : ICommand<ReportReviewResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record ReportReviewResult(Guid ReportId, Guid ReviewId);

public sealed class ReportReviewCommandHandler : ICommandHandler<ReportReviewCommand, ReportReviewResult>
{
    private readonly IReviewReadRepository _read;
    private readonly IReviewWriteRepository _write;

    public ReportReviewCommandHandler(IReviewReadRepository read, IReviewWriteRepository write)
    {
        _read = read;
        _write = write;
    }

    public async Task<ReportReviewResult> Handle(ReportReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _read.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException("این نظر پیدا نشد.");

        if (!review.IsPubliclyVisible)
            throw new InvalidAggregateStateException(
                nameof(Domain.Aggregates.Review), "Report", review.ModerationStatus.ToString(),
                "فقط نظرهای منتشرشده را می‌توانید گزارش کنید.");

        var reporter = UserId.From(request.ReporterId);
        if (await _write.HasReportedAsync(review.Id, reporter, cancellationToken))
            throw new ConflictException("این نظر را قبلاً گزارش کرده‌اید.");

        var report = ReviewReport.File(review.Id, reporter, request.Reason ?? string.Empty, DateTime.UtcNow);
        await _write.AddReportAsync(report, cancellationToken);
        return new ReportReviewResult(report.Id, review.Id);
    }
}
