// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderResult.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Result of removing a favorite provider
    /// </summary>
    public sealed record RemoveFavoriteProviderResult(
        Guid CustomerId,
        Guid ProviderId,
        DateTime RemovedAt,
        int RemainingFavorites);
}
