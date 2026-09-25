// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/DeleteException/DeleteExceptionCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.DeleteException;

public sealed record DeleteExceptionCommand(
    Guid ProviderId,
    Guid ExceptionId,
    Guid? IdempotencyKey = null) : ICommand<DeleteExceptionResult>;

public sealed record DeleteExceptionResult(
    bool Success,
    string Message);
