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

        var hasBreaks = command.BusinessHours.Any(dh => dh.Breaks != null && dh.Breaks.Any());

        if (hasBreaks)
        {
            // Use SetBusinessHoursWithBreaks for hours that include breaks
            var hoursWithBreaks = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close, IEnumerable<BreakPeriod>? Breaks)>();

            foreach (var dayDto in command.BusinessHours)
            {
                var day = (DayOfWeek)dayDto.DayOfWeek;

                if (!dayDto.IsOpen || dayDto.OpenTime == null || dayDto.CloseTime == null)
                {
                    hoursWithBreaks[day] = (null, null, null);
                    continue;
                }

                var openTime = new TimeOnly(dayDto.OpenTime.Hours, dayDto.OpenTime.Minutes);
                var closeTime = new TimeOnly(dayDto.CloseTime.Hours, dayDto.CloseTime.Minutes);

                // Convert breaks
                var breaks = dayDto.Breaks?.Select(b =>
                {
                    var start = new TimeOnly(b.Start.Hours, b.Start.Minutes);
                    var end = new TimeOnly(b.End.Hours, b.End.Minutes);
                    return BreakPeriod.Create(start, end);
                }).ToList() ?? new List<BreakPeriod>();

                hoursWithBreaks[day] = (openTime, closeTime, breaks);
            }

            provider.SetBusinessHoursWithBreaks(hoursWithBreaks);
        }
        else
        {
            // Use simple SetBusinessHours for hours without breaks
            var hours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

            foreach (var dayDto in command.BusinessHours)
            {
                var day = (DayOfWeek)dayDto.DayOfWeek;

                if (!dayDto.IsOpen || dayDto.OpenTime == null || dayDto.CloseTime == null)
                {
                    hours[day] = (null, null);
                    continue;
                }

                var openTime = new TimeOnly(dayDto.OpenTime.Hours, dayDto.OpenTime.Minutes);
                var closeTime = new TimeOnly(dayDto.CloseTime.Hours, dayDto.CloseTime.Minutes);

                hours[day] = (openTime, closeTime);
            }

            provider.SetBusinessHours(hours);
        }

        // Update business hours with breaks

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new UpdateBusinessHoursResult(
            Success: true,
            Message: "Business hours updated successfully");
    }
}
