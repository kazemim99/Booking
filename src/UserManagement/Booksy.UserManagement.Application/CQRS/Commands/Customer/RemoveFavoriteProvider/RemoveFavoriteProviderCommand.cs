// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Command to remove a provider from customer's favorites
    /// </summary>
    public sealed record RemoveFavoriteProviderCommand(
        Guid CustomerId,
        Guid ProviderId) : ICommand<RemoveFavoriteProviderResult>;
}
