// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Command to remove a provider from customer's favorites
    /// </summary>
    public sealed record RemoveFavoriteProviderCommand(
        Guid CustomerId,
        Guid ProviderId,Guid? IdempotencyKey = null) : ICommand<RemoveFavoriteProviderResult>;
}
