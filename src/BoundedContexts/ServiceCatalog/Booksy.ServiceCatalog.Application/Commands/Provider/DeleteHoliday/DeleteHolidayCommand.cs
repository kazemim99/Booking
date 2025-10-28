// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/DeleteHoliday/DeleteHolidayCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeleteHoliday;

public sealed record DeleteHolidayCommand(
    Guid ProviderId,
    Guid HolidayId,
    Guid? IdempotencyKey = null) : ICommand<DeleteHolidayResult>;

public sealed record DeleteHolidayResult(
    bool Success,
    string Message);
