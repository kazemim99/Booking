using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateWorkingHours;

public sealed class UpdateWorkingHoursCommandHandler : ICommandHandler<UpdateWorkingHoursCommand, UpdateWorkingHoursResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly ILogger<UpdateWorkingHoursCommandHandler> _logger;

    public UpdateWorkingHoursCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        ILogger<UpdateWorkingHoursCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _logger = logger;
    }

    public async Task<UpdateWorkingHoursResult> Handle(
        UpdateWorkingHoursCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating working hours for provider {ProviderId}",
            request.ProviderId);

        // Validate request
        ValidateRequest(request);

        // Get provider
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerWriteRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Convert request to dictionary for SetBusinessHours
        var hours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

        foreach (var (dayInt, dayHours) in request.BusinessHours)
        {
            var day = Enum.Parse<DayOfWeek>(dayInt);

            if (dayHours?.IsOpen == true && dayHours.OpenTime != null && dayHours.CloseTime != null)
            {
                var open = new TimeOnly(dayHours.OpenTime.Hours, dayHours.OpenTime.Minutes);
                var close = new TimeOnly(dayHours.CloseTime.Hours, dayHours.CloseTime.Minutes);
                hours[day] = (open, close);

                _logger.LogDebug(
                    "Set business hours for {DayOfWeek}: {OpenTime} - {CloseTime}",
                    day,
                    open,
                    close);
            }
            else
            {
                hours[day] = (null, null);
                _logger.LogDebug("Set {DayOfWeek} as closed", day);
            }
        }

        // Simple replacement - no complex tracking!
        provider.SetBusinessHours(hours);

        // Save changes
        await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

        var workingDaysCount = provider.BusinessHours.Count(h => h.IsOpen);

        _logger.LogInformation(
            "Working hours updated successfully for provider {ProviderId}. Working days: {WorkingDaysCount}",
            request.ProviderId,
            workingDaysCount);

        return new UpdateWorkingHoursResult(
            provider.Id.Value,
            workingDaysCount,
            DateTime.UtcNow);
    }

    private void ValidateRequest(UpdateWorkingHoursCommand request)
    {
        var errors = new Dictionary<string, List<string>>();

        // Validate at least one working day
        var hasWorkingDay = request.BusinessHours.Values.Any(h => h != null && h.IsOpen);
        if (!hasWorkingDay)
        {
            errors["businessHours"] = new List<string> { "At least one working day is required" };
        }

        // Validate business hours logic
        foreach (var (dayOfWeek, hours) in request.BusinessHours)
        {
            if (hours == null || !hours.IsOpen)
                continue;

            if (hours.OpenTime == null || hours.CloseTime == null)
            {
                var field = $"businessHours[{dayOfWeek}]";
                if (!errors.ContainsKey(field))
                    errors[field] = new List<string>();
                errors[field].Add("Day is marked as open but missing open/close times");
                continue;
            }

            // Validate time ranges
            if (hours.OpenTime.Hours < 0 || hours.OpenTime.Hours > 23)
            {
                var field = $"businessHours[{dayOfWeek}].openTime";
                if (!errors.ContainsKey(field))
                    errors[field] = new List<string>();
                errors[field].Add("Hours must be between 0 and 23");
            }

            if (hours.OpenTime.Minutes < 0 || hours.OpenTime.Minutes > 59)
            {
                var field = $"businessHours[{dayOfWeek}].openTime";
                if (!errors.ContainsKey(field))
                    errors[field] = new List<string>();
                errors[field].Add("Minutes must be between 0 and 59");
            }

            // Validate open time is before close time
            var openMinutes = hours.OpenTime.Hours * 60 + hours.OpenTime.Minutes;
            var closeMinutes = hours.CloseTime.Hours * 60 + hours.CloseTime.Minutes;

            if (openMinutes >= closeMinutes)
            {
                var field = $"businessHours[{dayOfWeek}]";
                if (!errors.ContainsKey(field))
                    errors[field] = new List<string>();
                errors[field].Add("Opening time must be before closing time");
            }

            // Validate breaks
            foreach (var (breakIndex, breakTime) in hours.Breaks.Select((b, i) => (i, b)))
            {
                var breakStart = breakTime.Start.Hours * 60 + breakTime.Start.Minutes;
                var breakEnd = breakTime.End.Hours * 60 + breakTime.End.Minutes;

                if (breakStart >= breakEnd)
                {
                    var field = $"businessHours[{dayOfWeek}].breaks[{breakIndex}]";
                    if (!errors.ContainsKey(field))
                        errors[field] = new List<string>();
                    errors[field].Add("Break start time must be before end time");
                }

                if (breakStart < openMinutes || breakEnd > closeMinutes)
                {
                    var field = $"businessHours[{dayOfWeek}].breaks[{breakIndex}]";
                    if (!errors.ContainsKey(field))
                        errors[field] = new List<string>();
                    errors[field].Add("Break times must be within working hours");
                }
            }
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}
