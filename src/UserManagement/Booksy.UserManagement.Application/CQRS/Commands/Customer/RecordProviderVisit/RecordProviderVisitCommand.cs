// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RecordProviderVisit/RecordProviderVisitCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RecordProviderVisit
{
    /// <summary>
    /// Command to record a provider visit/view by a customer
    /// </summary>
    public sealed record RecordProviderVisitCommand(
        Guid CustomerId,
        Guid ProviderId,
        string? ViewSource = null) : ICommand<RecordProviderVisitResult>
    {
        public Guid? IdempotencyKey { get; set; }
    }
}
