// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/AddException/AddExceptionCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.AddException;

public sealed record AddExceptionCommand(
    Guid ProviderId,
    DateOnly Date,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime,
    string Reason,
    Guid? IdempotencyKey = null) : ICommand<AddExceptionResult>;

public sealed record AddExceptionResult(
    Guid ExceptionId,
    bool Success,
    string Message);
