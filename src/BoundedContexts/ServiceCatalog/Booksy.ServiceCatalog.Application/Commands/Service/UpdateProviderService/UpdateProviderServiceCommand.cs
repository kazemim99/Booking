using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateProviderService;

public sealed record UpdateProviderServiceCommand(
    Guid ServiceId,
    Guid ProviderId,
    string ServiceName,
    string? Description,
    int DurationHours,
    int DurationMinutes,
    decimal Price,
    string Currency,
    string? Category,
    bool IsMobileService,
    Guid? IdempotencyKey = null) : ICommand<UpdateProviderServiceResult>;

public sealed record UpdateProviderServiceResult(
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int TotalDurationMinutes,
    DateTime UpdatedAt);
