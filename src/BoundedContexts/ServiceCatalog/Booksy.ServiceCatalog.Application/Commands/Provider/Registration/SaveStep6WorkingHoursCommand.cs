using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 6: Save working hours to provider draft
/// Requires Step 3 to be completed (provider draft must exist)
/// </summary>
public sealed record SaveStep6WorkingHoursCommand(
    Guid ProviderId,
    List<DayHoursDto> BusinessHours,
    Guid? IdempotencyKey = null
) : ICommand<SaveStep6WorkingHoursResult>;

public sealed record DayHoursDto(
    int DayOfWeek, // 0 = Sunday, 1 = Monday, etc.
    bool IsOpen,
    TimeSlotDto? OpenTime,
    TimeSlotDto? CloseTime,
    List<BreakTimeDto> Breaks
);

public sealed record TimeSlotDto(
    int Hours,
    int Minutes
);

public sealed record BreakTimeDto(
    TimeSlotDto Start,
    TimeSlotDto End
);

public sealed record SaveStep6WorkingHoursResult(
    Guid ProviderId,
    int RegistrationStep,
    int OpenDaysCount,
    string Message
);
