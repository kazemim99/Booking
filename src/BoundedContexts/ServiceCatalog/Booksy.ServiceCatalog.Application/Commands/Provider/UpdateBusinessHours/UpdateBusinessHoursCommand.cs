// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessHours/UpdateBusinessHoursCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessHours;

public sealed record UpdateBusinessHoursCommand(
    Guid ProviderId,
    List<BusinessHoursDayDto> BusinessHours,
    Guid? IdempotencyKey = null) : ICommand<UpdateBusinessHoursResult>;

public sealed record BusinessHoursDayDto(
    int DayOfWeek,
    bool IsOpen,
    string? OpenTime,  // "HH:mm" format
    string? CloseTime, // "HH:mm" format
    List<BreakPeriodDto>? Breaks);

public sealed record BreakPeriodDto(
    string StartTime, // "HH:mm" format
    string EndTime,   // "HH:mm" format
    string? Label);

public sealed record UpdateBusinessHoursResult(
    bool Success,
    string Message);
