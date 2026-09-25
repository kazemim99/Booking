using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Promotions
{
    // What customers see: the salon page's offers and the price of a visit before they confirm it.

    /// <summary>
    /// Automatic offers in force at a salon right now, for its public page. Codes are never listed. Conditions that
    /// depend on the customer or the appointment (new customer, day, time, minimum) travel as data for the client to
    /// describe; the quote decides whether they hold for a particular visit.
    /// </summary>
    public sealed record GetProviderOffersQuery(Guid ProviderId) : IQuery<IReadOnlyList<PublicOfferDto>>;

    public sealed class GetProviderOffersQueryHandler : IQueryHandler<GetProviderOffersQuery, IReadOnlyList<PublicOfferDto>>
    {
        private readonly IPromotionRepository _promotions;

        public GetProviderOffersQueryHandler(IPromotionRepository promotions) => _promotions = promotions;

        public async Task<IReadOnlyList<PublicOfferDto>> Handle(GetProviderOffersQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var candidates = await _promotions.GetPricingCandidatesAsync(ProviderId.From(request.ProviderId), now, cancellationToken);

            return candidates
                .Where(c => c.IsEnrolled
                            && c.Promotion.Activation == PromotionActivation.Automatic
                            && c.Promotion.StateAt(now) == PromotionState.Active)
                .Select(c => c.Promotion)
                .OrderByDescending(p => p.DiscountKind == DiscountKind.Percentage ? p.DiscountValue : 0m)
                .ThenBy(p => p.EndsAt ?? DateTime.MaxValue)
                .Select(PublicOfferDto.From)
                .ToList();
        }
    }

    /// <summary>
    /// The price of a visit as it would be booked now: subtotal, the one discount the server would apply, total, and
    /// what happened to the entered code. Same pricing path as booking creation.
    /// </summary>
    public sealed record QuoteBookingPriceQuery(
        Guid CustomerId,
        Guid ProviderId,
        IReadOnlyList<Guid> ServiceIds,
        DateTime StartTime,
        string? PromotionCode) : IQuery<PriceQuoteDto>;

    public sealed class QuoteBookingPriceQueryHandler : IQueryHandler<QuoteBookingPriceQuery, PriceQuoteDto>
    {
        private readonly IServiceReadRepository _services;
        private readonly IPromotionPricingService _pricing;

        public QuoteBookingPriceQueryHandler(IServiceReadRepository services, IPromotionPricingService pricing)
        {
            _services = services;
            _pricing = pricing;
        }

        public async Task<PriceQuoteDto> Handle(QuoteBookingPriceQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var ids = request.ServiceIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0)
                throw new DomainValidationException(nameof(request.ServiceIds), "حداقل یک خدمت انتخاب کنید.");

            var lines = new List<PricedLine>(ids.Count);
            string? currency = null;
            foreach (var id in ids)
            {
                var service = await _services.GetByIdAsync(ServiceId.From(id), cancellationToken);
                if (service is null || service.ProviderId != providerId)
                    throw new NotFoundException("این خدمت پیدا نشد.");
                currency ??= service.BasePrice.Currency;
                lines.Add(new PricedLine(id, service.BasePrice.Amount));
            }

            var pricing = await _pricing.PriceAsync(new PricingRequest(
                providerId, lines, currency!, request.StartTime, request.CustomerId, request.PromotionCode,
                DateTime.UtcNow), cancellationToken);

            return PriceQuoteDto.From(pricing.Quote);
        }
    }
}
