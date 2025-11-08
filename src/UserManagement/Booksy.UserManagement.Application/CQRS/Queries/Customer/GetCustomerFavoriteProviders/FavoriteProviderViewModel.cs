// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/FavoriteProviderViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    public sealed class FavoriteProviderViewModel
    {
        public Guid ProviderId { get; init; }
        public string? Notes { get; init; }
        public DateTime AddedAt { get; init; }
    }
}
