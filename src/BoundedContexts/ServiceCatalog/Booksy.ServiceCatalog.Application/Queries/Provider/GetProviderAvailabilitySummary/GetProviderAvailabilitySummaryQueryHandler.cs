using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilitySummary
{
    /// <summary>
    /// Counts the free times each salon has on the first day it has any.
    ///
    /// <para>The count is for the salon's SHORTEST active service: it is the most anyone could
    /// book, and it answers the card's question ("is this place free soon?") without picking a
    /// service on the customer's behalf. A salon that cannot be booked at all reports zero rather
    /// than failing the whole list — one misconfigured salon must not blank every card.</para>
    /// </summary>
    public sealed class GetProviderAvailabilitySummaryQueryHandler
        : IQueryHandler<GetProviderAvailabilitySummaryQuery, IReadOnlyList<ProviderAvailabilitySummary>>
    {
        private readonly IProviderReadRepository _providers;
        private readonly IServiceReadRepository _services;
        private readonly IAvailabilityService _availability;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GetProviderAvailabilitySummaryQueryHandler> _logger;

        /// Walking a salon's calendar is not cheap, and a list of cards asks about the same salons
        /// on every load. Two minutes is short enough that a booking made now still shows.
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(2);

        public GetProviderAvailabilitySummaryQueryHandler(
            IProviderReadRepository providers,
            IServiceReadRepository services,
            IAvailabilityService availability,
            IMemoryCache cache,
            ILogger<GetProviderAvailabilitySummaryQueryHandler> logger)
        {
            _providers = providers;
            _services = services;
            _availability = availability;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProviderAvailabilitySummary>> Handle(
            GetProviderAvailabilitySummaryQuery request,
            CancellationToken cancellationToken)
        {
            var summaries = new List<ProviderAvailabilitySummary>();
            foreach (var providerId in request.ProviderIds.Distinct())
            {
                var key = $"availability-summary:{providerId}:{request.DaysAhead}:{DateTime.UtcNow:yyyyMMdd}";
                if (_cache.TryGetValue(key, out ProviderAvailabilitySummary? cached) && cached is not null)
                {
                    summaries.Add(cached);
                    continue;
                }

                var summary = await SummariseAsync(providerId, request.DaysAhead, cancellationToken);
                _cache.Set(key, summary, CacheFor);
                summaries.Add(summary);
            }
            return summaries;
        }

        private async Task<ProviderAvailabilitySummary> SummariseAsync(
            Guid providerId, int daysAhead, CancellationToken cancellationToken)
        {
            var none = new ProviderAvailabilitySummary(providerId, null, 0, null);
            try
            {
                var provider = await _providers.GetByIdAsync(ProviderId.From(providerId), cancellationToken);
                if (provider == null)
                    return none;

                var services = await _services.GetByProviderIdAndStatusAsync(
                    provider.Id, ServiceStatus.Active, cancellationToken);
                var service = services.OrderBy(s => s.Duration.Value).FirstOrDefault();
                if (service == null)
                    return none;

                var day = SalonTime.Now.Date;
                for (var i = 0; i < daysAhead; i++, day = day.AddDays(1))
                {
                    var slots = await _availability.GetAvailableTimeSlotsAsync(
                        provider, service, day, null, cancellationToken: cancellationToken);

                    var starts = slots.Select(s => s.StartTime).Distinct().OrderBy(s => s).ToList();
                    if (starts.Count > 0)
                        return new ProviderAvailabilitySummary(providerId, day, starts.Count, starts.First());
                }

                return none;
            }
            catch (Exception ex)
            {
                // A card that cannot say "free times" is a smaller loss than a list that fails.
                _logger.LogWarning(ex, "Could not summarise availability for provider {ProviderId}", providerId);
                return none;
            }
        }
    }
}
