// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/AddPriceTier/AddPriceTierResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Service.AddPriceTier
{
    public sealed record AddPriceTierResult(
        Guid ServiceId,
        Guid TierId,
        string Name,
        decimal Price,
        string Currency,
        bool IsDefault,
        DateTime CreatedAt);
}