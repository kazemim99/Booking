// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessHours/UpdateBusinessHoursCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessHours;

public sealed class UpdateBusinessHoursCommandHandler
    : ICommandHandler<UpdateBusinessHoursCommand, UpdateBusinessHoursResult>
{
    private readonly IProviderWriteRepository _providerRepository;

    public UpdateBusinessHoursCommandHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<UpdateBusinessHoursResult> Handle(
        UpdateBusinessHoursCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Convert DTOs to domain model with breaks
        var hoursWithBreaks = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close, IEnumerable<BreakPeriod>? Breaks)>();

        foreach (var dayDto in command.BusinessHours)
        {
            var dayOfWeek = (DayOfWeek)dayDto.DayOfWeek;

            if (dayDto.IsOpen && dayDto.OpenTime != null && dayDto.CloseTime != null)
            {
                var openTime = TimeOnly.Parse(dayDto.OpenTime);
                var closeTime = TimeOnly.Parse(dayDto.CloseTime);

                // Validate time range
                if (openTime >= closeTime)
                    throw new DomainValidationException($"Open time must be before close time for {dayOfWeek}");

                // Parse breaks if provided
                IEnumerable<BreakPeriod>? breaks = null;
                if (dayDto.Breaks?.Any() == true)
                {
                    breaks = dayDto.Breaks.Select(b =>
                        BreakPeriod.Create(
                            TimeOnly.Parse(b.StartTime),
                            TimeOnly.Parse(b.EndTime),
                            b.Label)).ToList();
                }

                hoursWithBreaks[dayOfWeek] = (openTime, closeTime, breaks);
            }
            else
            {
                hoursWithBreaks[dayOfWeek] = (null, null, null);
            }
        }

        // Update business hours with breaks
        provider.SetBusinessHoursWithBreaks(hoursWithBreaks);

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new UpdateBusinessHoursResult(
            Success: true,
            Message: "Business hours updated successfully");
    }
}
