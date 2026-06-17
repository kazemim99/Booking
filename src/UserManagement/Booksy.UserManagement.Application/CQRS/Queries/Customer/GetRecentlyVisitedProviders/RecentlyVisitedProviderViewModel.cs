// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/RecentlyVisitedProviderViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetRecentlyVisitedProviders
{
    /// <summary>
    /// View model representing a recently visited provider
    /// </summary>
    public sealed class RecentlyVisitedProviderViewModel
    {
        public Guid ProviderId { get; init; }
        public DateTime VisitedAt { get; init; }
        public string? ViewSource { get; init; }
    }
}
