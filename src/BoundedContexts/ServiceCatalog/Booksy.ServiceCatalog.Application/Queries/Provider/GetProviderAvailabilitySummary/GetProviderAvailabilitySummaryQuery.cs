using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilitySummary
{
    /// <summary>
    /// "How soon, and how many times, can I book here?" for a list of salons at once — what a
    /// results card shows. One request per card would be one round trip per salon on screen.
    /// </summary>
    /// <param name="DaysAhead">How far to look for the first day with free times.</param>
    public sealed record GetProviderAvailabilitySummaryQuery(
        IReadOnlyList<Guid> ProviderIds,
        int DaysAhead = 7) : IQuery<IReadOnlyList<ProviderAvailabilitySummary>>;

    /// <param name="Date">The first day with free times; null when none was found.</param>
    /// <param name="FreeSlotCount">Free start times on that day.</param>
    /// <param name="FirstFreeTime">The earliest of them, as salon wall-clock.</param>
    public sealed record ProviderAvailabilitySummary(
        Guid ProviderId,
        DateTime? Date,
        int FreeSlotCount,
        DateTime? FirstFreeTime);
}
