// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/AddFavoriteProvider/AddFavoriteProviderResult.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider
{
    /// <summary>
    /// Result of adding a favorite provider
    /// </summary>
    public sealed record AddFavoriteProviderResult(
        Guid CustomerId,
        Guid ProviderId,
        DateTime AddedAt,
        int TotalFavorites);
}
