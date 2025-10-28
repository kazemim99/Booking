// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetBusinessHours/GetBusinessHoursQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetBusinessHours;

public sealed class GetBusinessHoursQueryHandler
    : IQueryHandler<GetBusinessHoursQuery, BusinessHoursViewModel>
{
    private readonly IProviderWriteRepository _providerRepository;

    public GetBusinessHoursQueryHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<BusinessHoursViewModel> Handle(
        GetBusinessHoursQuery query,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(query.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Map business hours to view model
        var businessHoursList = new List<BusinessHoursDayViewModel>();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var businessHours = provider.GetBusinessHoursFor(day);

            List<BreakPeriodViewModel>? breaks = null;
            if (businessHours?.Breaks?.Any() == true)
            {
                breaks = businessHours.Breaks.Select(b => new BreakPeriodViewModel(
                    StartTime: b.StartTime.ToString("HH:mm"),
                    EndTime: b.EndTime.ToString("HH:mm"),
                    Label: b.Label)).ToList();
            }

            businessHoursList.Add(new BusinessHoursDayViewModel(
                DayOfWeek: (int)day,
                DayName: day.ToString(),
                IsOpen: businessHours?.IsOpen ?? false,
                OpenTime: businessHours?.OpenTime?.ToString("HH:mm"),
                CloseTime: businessHours?.CloseTime?.ToString("HH:mm"),
                Breaks: breaks));
        }

        return new BusinessHoursViewModel(businessHoursList);
    }
}
