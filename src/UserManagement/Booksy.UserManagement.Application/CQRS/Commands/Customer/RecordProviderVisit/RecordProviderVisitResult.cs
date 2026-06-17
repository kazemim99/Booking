// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RecordProviderVisit/RecordProviderVisitResult.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RecordProviderVisit
{
    /// <summary>
    /// Result of recording a provider visit
    /// </summary>
    public sealed record RecordProviderVisitResult(
        Guid CustomerId,
        Guid ProviderId,
        DateTime VisitedAt,
        string? ViewSource,
        int TotalRecentlyVisited);
}
