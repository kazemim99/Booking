using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Services.Reviews;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.RecomputeProviderRatings;

/// <summary>
/// Recompute every provider's rating from their published reviews. Run once after the moderation migration,
/// and safe to run again at any time.
/// </summary>
public sealed record RecomputeProviderRatingsCommand : ICommand<RecomputeProviderRatingsResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record RecomputeProviderRatingsResult(int ProvidersRecomputed);

public sealed class RecomputeProviderRatingsCommandHandler
    : ICommandHandler<RecomputeProviderRatingsCommand, RecomputeProviderRatingsResult>
{
    private readonly IProviderRatingRecomputer _recomputer;
    private readonly ILogger<RecomputeProviderRatingsCommandHandler> _logger;

    public RecomputeProviderRatingsCommandHandler(
        IProviderRatingRecomputer recomputer,
        ILogger<RecomputeProviderRatingsCommandHandler> logger)
    {
        _recomputer = recomputer;
        _logger = logger;
    }

    public async Task<RecomputeProviderRatingsResult> Handle(
        RecomputeProviderRatingsCommand request, CancellationToken cancellationToken)
    {
        var count = await _recomputer.RecomputeAllAsync(cancellationToken);
        _logger.LogInformation("Recomputed ratings for {ProviderCount} providers", count);
        return new RecomputeProviderRatingsResult(count);
    }
}
