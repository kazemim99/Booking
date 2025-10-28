using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateWorkingHours;

public sealed record UpdateWorkingHoursCommand(
    Guid ProviderId,
    Dictionary<string, DayHoursDto?> BusinessHours,
    Guid? IdempotencyKey = null) : ICommand<UpdateWorkingHoursResult>;

public sealed record DayHoursDto(
    int DayOfWeek,
    bool IsOpen,
    TimeSlotDto? OpenTime,
    TimeSlotDto? CloseTime,
    List<BreakTimeDto> Breaks);

public sealed record TimeSlotDto(int Hours, int Minutes);

public sealed record BreakTimeDto(TimeSlotDto Start, TimeSlotDto End);

public sealed record UpdateWorkingHoursResult(
    Guid ProviderId,
    int WorkingDaysCount,
    DateTime UpdatedAt);
