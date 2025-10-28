// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetHolidays/GetHolidaysQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetHolidays;

public sealed class GetHolidaysQueryHandler
    : IQueryHandler<GetHolidaysQuery, HolidaysViewModel>
{
    private readonly IProviderWriteRepository _providerRepository;

    public GetHolidaysQueryHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<HolidaysViewModel> Handle(
        GetHolidaysQuery query,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(query.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Map holidays to view model
        var holidays = provider.Holidays
            .OrderBy(h => h.Date)
            .Select(h => new HolidayViewModel(
                Id: h.Id,
                Date: h.Date.ToString("yyyy-MM-dd"),
                Reason: h.Reason,
                IsRecurring: h.IsRecurring,
                Pattern: h.Pattern?.ToString()))
            .ToList();

        return new HolidaysViewModel(holidays);
    }
}
