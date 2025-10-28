// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetExceptions/GetExceptionsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetExceptions;

public sealed record GetExceptionsQuery(
    Guid ProviderId) : IQuery<ExceptionsViewModel>;

public sealed record ExceptionsViewModel(
    List<ExceptionViewModel> Exceptions);

public sealed record ExceptionViewModel(
    Guid Id,
    string Date,
    string? OpenTime,
    string? CloseTime,
    string Reason,
    bool IsClosed);
