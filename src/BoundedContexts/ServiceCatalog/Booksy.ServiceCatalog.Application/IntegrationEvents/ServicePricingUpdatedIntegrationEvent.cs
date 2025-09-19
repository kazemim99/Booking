// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/ServicePricingUpdatedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
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