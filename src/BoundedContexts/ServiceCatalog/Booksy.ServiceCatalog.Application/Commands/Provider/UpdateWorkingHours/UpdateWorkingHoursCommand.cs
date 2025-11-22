using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateWorkingHours;

public sealed record UpdateWorkingHoursCommand(
    Guid ProviderId,
    Dictionary<string, DayHoursDto?> BusinessHours,
    Guid? IdempotencyKey = null) : ICommand<UpdateWorkingHoursResult>;



public sealed record TimeSlotDto(int Hours, int Minutes);

public sealed record BreakTimeDto(TimeSlotDto Start, TimeSlotDto End);

public sealed record UpdateWorkingHoursResult(
    Guid ProviderId,
    int WorkingDaysCount,
    DateTime UpdatedAt);
