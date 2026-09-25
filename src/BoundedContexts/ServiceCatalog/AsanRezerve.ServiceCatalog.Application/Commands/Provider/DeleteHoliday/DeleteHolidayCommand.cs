// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/DeleteHoliday/DeleteHolidayCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.DeleteHoliday;

public sealed record DeleteHolidayCommand(
    Guid ProviderId,
    Guid HolidayId,
    Guid? IdempotencyKey = null) : ICommand<DeleteHolidayResult>;

public sealed record DeleteHolidayResult(
    bool Success,
    string Message);
