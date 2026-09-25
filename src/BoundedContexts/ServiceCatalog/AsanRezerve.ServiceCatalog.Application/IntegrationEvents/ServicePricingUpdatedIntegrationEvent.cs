// ========================================
// AsanRezerve.ServiceCatalog.Application/IntegrationEvents/ServicePricingUpdatedIntegrationEvent.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.ServiceCatalog.Application.IntegrationEvents
{
    public sealed record ServicePricingUpdatedIntegrationEvent(
        Guid ServiceId,
        Guid ProviderId,
        string ServiceName,
        decimal OldPrice,
        decimal NewPrice,
        string Currency,
        bool RequiresDeposit,
        decimal DepositPercentage,
        DateTime UpdatedAt) : IntegrationEvent;
}