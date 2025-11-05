using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep6WorkingHoursCommandHandler
    : ICommandHandler<SaveStep6WorkingHoursCommand, SaveStep6WorkingHoursResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep6WorkingHoursCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep6WorkingHoursResult> Handle(
        SaveStep6WorkingHoursCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new InvalidOperationException("Provider not found");

        if (provider.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to update this provider");

        if (provider.Status != ProviderStatus.Drafted)
            throw new InvalidOperationException("Provider is not in draft status");

        if (!request.BusinessHours.Any())
            throw new InvalidOperationException("Business hours are required");

        // Check if any breaks are included
        var hasBreaks = request.BusinessHours.Any(dh => dh.Breaks != null && dh.Breaks.Any());

        if (hasBreaks)
        {
            // Use SetBusinessHoursWithBreaks for hours that include breaks
            var hoursWithBreaks = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close, IEnumerable<BreakPeriod>? Breaks)>();

            foreach (var dayDto in request.BusinessHours)
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

            foreach (var dayDto in request.BusinessHours)
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

        // Update registration step
        provider.UpdateRegistrationStep(6);

        await _unitOfWork.CommitAsync(cancellationToken);

        var openDaysCount = provider.BusinessHours.Count(h => h.IsOpen);

        return new SaveStep6WorkingHoursResult(
            provider.Id.Value,
            6,
            openDaysCount,
            $"Business hours saved successfully. Open {openDaysCount} day(s) per week.");
    }
}
