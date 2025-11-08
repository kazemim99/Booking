// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/AddFavoriteProvider/AddFavoriteProviderCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider
{
    /// <summary>
    /// Command to add a provider to customer's favorites
    /// </summary>
    public sealed record AddFavoriteProviderCommand(
        Guid CustomerId,
        Guid ProviderId,
        string? Notes = null) : ICommand<AddFavoriteProviderResult>;
}
