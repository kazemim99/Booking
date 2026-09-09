// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payout/GetPayoutById/GetPayoutByIdQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Payout.GetPayoutById
{
    /// <summary>
    /// Looks the payout up by its own key.
    ///
    /// Before this existed, <c>GET /payouts/{id}</c> asked for every payout of provider
    /// <c>Guid.Empty</c> and filtered the result in the controller — and since <c>ProviderId</c>
    /// rejects an empty guid, the endpoint threw <see cref="ArgumentException"/> on every call.
    /// </summary>
    public sealed class GetPayoutByIdQueryHandler : IQueryHandler<GetPayoutByIdQuery, PayoutDetailsDto?>
    {
        private readonly IPayoutReadRepository _payoutRepository;

        public GetPayoutByIdQueryHandler(IPayoutReadRepository payoutRepository)
        {
            _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        }

        public async Task<PayoutDetailsDto?> Handle(GetPayoutByIdQuery request, CancellationToken cancellationToken)
        {
            var payout = await _payoutRepository.GetByIdAsync(PayoutId.From(request.PayoutId), cancellationToken);

            return payout is null ? null : PayoutDetailsDto.From(payout);
        }
    }
}
