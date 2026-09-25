// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetHolidays/GetHolidaysQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetHolidays;

public sealed record GetHolidaysQuery(
    Guid ProviderId) : IQuery<HolidaysViewModel>;

public sealed record HolidaysViewModel(
    List<HolidayViewModel> Holidays);

public sealed record HolidayViewModel(
    Guid Id,
    string Date,
    string Reason,
    bool IsRecurring,
    string? Pattern);
