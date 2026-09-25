using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.ReportReview;

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
                     ?? throw new NotFoundException($"Review with ID {request.ReviewId} not found");

        if (!review.IsPubliclyVisible)
            throw new InvalidAggregateStateException(typeof(Domain.Aggregates.Review), "Report", review.ModerationStatus.ToString());

        var reporter = UserId.From(request.ReporterId);
        if (await _write.HasReportedAsync(review.Id, reporter, cancellationToken))
            throw new ConflictException("You have already reported this review");

        var report = ReviewReport.File(review.Id, reporter, request.Reason ?? string.Empty, DateTime.UtcNow);
        await _write.AddReportAsync(report, cancellationToken);
        return new ReportReviewResult(report.Id, review.Id);
    }
}
