// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Payout/GetProviderPayouts/GetProviderPayoutsQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts
{
    public sealed class GetProviderPayoutsQueryHandler : IQueryHandler<GetProviderPayoutsQuery, List<PayoutDetailsDto>>
    {
        private readonly IPayoutReadRepository _payoutRepository;

        public GetProviderPayoutsQueryHandler(IPayoutReadRepository payoutRepository)
        {
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        }

        public async Task<List<PayoutDetailsDto>> Handle(GetProviderPayoutsQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            IReadOnlyList<Domain.Aggregates.PayoutAggregate.Payout> payouts;

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                payouts = await _payoutRepository.GetProviderPayoutsInRangeAsync(
                    providerId,
                    request.StartDate.Value,
                    request.EndDate.Value,
                    cancellationToken);
            }
            else
            {
                payouts = await _payoutRepository.GetByProviderIdAsync(providerId, cancellationToken);
            }

            // Apply status filter
            var filtered = payouts.AsEnumerable();
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PayoutStatus>(request.Status, out var status))
            {
                filtered = filtered.Where(p => p.Status == status);
            }

            return filtered
                .Select(PayoutDetailsDto.From)
                .ToList();
        }
    }
}
