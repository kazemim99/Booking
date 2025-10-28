// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetBusinessHours/GetBusinessHoursQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetBusinessHours;

public sealed record GetBusinessHoursQuery(
    Guid ProviderId) : IQuery<BusinessHoursViewModel>;

public sealed record BusinessHoursViewModel(
    List<BusinessHoursDayViewModel> BusinessHours);

public sealed record BusinessHoursDayViewModel(
    int DayOfWeek,
    string DayName,
    bool IsOpen,
    string? OpenTime,
    string? CloseTime,
    List<BreakPeriodViewModel>? Breaks);

public sealed record BreakPeriodViewModel(
    string StartTime,
    string EndTime,
    string? Label);
