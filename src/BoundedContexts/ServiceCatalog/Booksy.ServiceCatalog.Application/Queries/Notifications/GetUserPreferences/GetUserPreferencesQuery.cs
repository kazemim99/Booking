// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetUserPreferences/GetUserPreferencesQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetUserPreferences
{
    /// <summary>
    /// Query to get user notification preferences
    /// </summary>
    public sealed record GetUserPreferencesQuery(
        Guid UserId) : IQuery<UserPreferencesViewModel?>;
}
