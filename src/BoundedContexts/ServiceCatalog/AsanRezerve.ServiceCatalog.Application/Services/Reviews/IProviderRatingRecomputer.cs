using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Services.Reviews
{
    /// <summary>
    /// Recomputes a provider's rating from their published reviews and stages it on the current unit of work.
    /// </summary>
    /// <remarks>
    /// <para><b>Call it inline, from the command handler, after the review change and before the handler
    /// returns.</b> Not from a domain event handler: the unit of work dispatches events before it saves, on a fresh
    /// DI scope and connection, outside the command's transaction — a handler there would read the review as it
    /// was before the change and leave every rating one action stale (design D6).</para>
    ///
    /// <para>It sees the command's unsaved changes: reviews the caller has already modified in this unit of work
    /// are taken as they are in memory, not as the database last stored them. So the caller does not need to
    /// flush first, and a rolled-back command rolls the rating back with it.</para>
    ///
    /// <para>Recompute, never increment: the whole published set is re-read every time, so there is nothing to
    /// drift.</para>
    /// </remarks>
    public interface IProviderRatingRecomputer
    {
        Task RecomputeAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recomputes every provider. The post-migration backfill, and a repair tool: it converges on the truth
        /// from any starting state, so running it twice changes nothing the second time.
        /// </summary>
        /// <returns>How many providers were recomputed.</returns>
        Task<int> RecomputeAllAsync(CancellationToken cancellationToken = default);
    }
}
