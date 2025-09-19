// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/AddPriceTier/AddPriceTierCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.AddPriceTier
{
    public sealed record AddPriceTierCommand(
        Guid ServiceId,
        string Name,
        string? Description,
        decimal Price,
        string Currency,
        bool IsDefault = false,
        Dictionary<string, string>? Attributes = null,
        Guid? IdempotencyKey = null) : ICommand<AddPriceTierResult>;
}