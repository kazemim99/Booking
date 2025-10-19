using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;

public sealed record DeleteProviderServiceCommand(
    Guid ServiceId,
    Guid ProviderId,
    Guid? IdempotencyKey = null) : ICommand<DeleteProviderServiceResult>;

public sealed record DeleteProviderServiceResult(
    Guid ServiceId,
    bool Success,
    string Message);
