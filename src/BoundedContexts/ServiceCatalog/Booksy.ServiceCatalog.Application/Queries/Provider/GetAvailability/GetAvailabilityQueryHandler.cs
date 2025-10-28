// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetAvailability/GetAvailabilityQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetAvailability;

public sealed class GetAvailabilityQueryHandler
    : IQueryHandler<GetAvailabilityQuery, AvailabilityViewModel>
{
    private readonly IProviderWriteRepository _providerRepository;

    public GetAvailabilityQueryHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<AvailabilityViewModel> Handle(
        GetAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(query.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Get availability for the date
        var (isAvailable, reason, slots) = provider.GetAvailabilityForDate(query.Date);

        // Map to view model
        var slotViewModels = slots.Select(slot => new TimeSlotViewModel(
            StartTime: slot.Start.ToString("HH:mm"),
            EndTime: slot.End.ToString("HH:mm"))).ToList();

        return new AvailabilityViewModel(
            Date: query.Date.ToString("yyyy-MM-dd"),
            IsAvailable: isAvailable,
            Reason: reason,
            Slots: slotViewModels);
    }
}
