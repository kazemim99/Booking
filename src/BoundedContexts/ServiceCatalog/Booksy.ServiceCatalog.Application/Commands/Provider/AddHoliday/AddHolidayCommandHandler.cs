// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddHoliday/AddHolidayCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Entities;
using HolidaySchedule = Booksy.ServiceCatalog.Domain.Entities.HolidaySchedule;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddHoliday;

public sealed class AddHolidayCommandHandler
    : ICommandHandler<AddHolidayCommand, AddHolidayResult>
{
    private readonly IProviderWriteRepository _providerRepository;

    public AddHolidayCommandHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<AddHolidayResult> Handle(
        AddHolidayCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Add holiday
        HolidaySchedule holiday;
        if (command.IsRecurring && !string.IsNullOrEmpty(command.Pattern))
        {
            if (!Enum.TryParse<RecurrencePattern>(command.Pattern, out var pattern))
                throw new DomainValidationException($"Invalid recurrence pattern: {command.Pattern}");

            holiday = provider.AddRecurringHoliday(command.Date, command.Reason, pattern);
        }
        else
        {
            if (command.IsRecurring)
                throw new DomainValidationException("Recurrence pattern is required for recurring holidays");

            holiday = provider.AddHoliday(command.Date, command.Reason);
        }

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new AddHolidayResult(
            HolidayId: holiday.Id,
            Success: true,
            Message: "Holiday added successfully");
    }
}
