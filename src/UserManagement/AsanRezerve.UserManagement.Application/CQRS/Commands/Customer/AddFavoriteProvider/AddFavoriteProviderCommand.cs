// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/Customer/AddFavoriteProvider/AddFavoriteProviderCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider
{
    /// <summary>
    /// Command to add a provider to customer's favorites
    /// </summary>
    public sealed record AddFavoriteProviderCommand(
        Guid CustomerId,
        Guid ProviderId,
        string? Notes = null, Guid? IdempotencyKey=null) : ICommand<AddFavoriteProviderResult>;
}
