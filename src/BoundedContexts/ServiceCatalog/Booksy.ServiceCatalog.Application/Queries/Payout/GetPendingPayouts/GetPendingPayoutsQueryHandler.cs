// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetPendingPayouts/GetPendingPayoutsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts
{
    public sealed class GetPendingPayoutsQueryHandler : IQueryHandler<GetPendingPayoutsQuery, List<PayoutSummaryDto>>
    {
        private readonly IPayoutReadRepository _payoutRepository;

        public GetPendingPayoutsQueryHandler(IPayoutReadRepository payoutRepository)
        {
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        }

        public async Task<List<PayoutSummaryDto>> Handle(GetPendingPayoutsQuery request, CancellationToken cancellationToken)
        {
            var payouts = await _payoutRepository.GetPendingPayoutsAsync(request.BeforeDate, cancellationToken);

            return payouts
                .Select(p => new PayoutSummaryDto(
                    p.Id.Value,
                    p.ProviderId.Value,
                    p.GrossAmount.Amount,
                    p.CommissionAmount.Amount,
                    p.NetAmount.Amount,
                    p.NetAmount.Currency,
                    p.PeriodStart,
                    p.PeriodEnd,
                    p.PaymentIds.Count,
                    p.Status.ToString(),
                    p.CreatedAt,
                    p.ScheduledAt))
                .ToList();
        }
    }
}
