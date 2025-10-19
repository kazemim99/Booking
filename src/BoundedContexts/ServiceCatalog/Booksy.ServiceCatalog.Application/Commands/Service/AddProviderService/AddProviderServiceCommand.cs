using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.AddProviderService;

public sealed record AddProviderServiceCommand(
    Guid ProviderId,
    string ServiceName,
    string? Description,
    int DurationHours,
    int DurationMinutes,
    decimal Price,
    string Currency,
    string? Category,
    bool IsMobileService,
    Guid? IdempotencyKey = null) : ICommand<AddProviderServiceResult>;

public sealed record AddProviderServiceResult(
    Guid ServiceId,
    Guid ProviderId,
    string ServiceName,
    decimal Price,
    string Currency,
    int TotalDurationMinutes,
    DateTime CreatedAt);
