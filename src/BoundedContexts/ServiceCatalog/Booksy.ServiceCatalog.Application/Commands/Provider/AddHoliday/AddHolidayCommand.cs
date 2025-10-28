// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddHoliday/AddHolidayCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddHoliday;

public sealed record AddHolidayCommand(
    Guid ProviderId,
    DateOnly Date,
    string Reason,
    bool IsRecurring,
    string? Pattern,
    Guid? IdempotencyKey = null) : ICommand<AddHolidayResult>;

public sealed record AddHolidayResult(
    Guid HolidayId,
    bool Success,
    string Message);
