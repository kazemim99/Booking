// ========================================
// Application/Commands/Provider/DeactivateProviderStaff/DeactivateProviderStaffCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.DeactivateProviderStaff
{
    /// <summary>
    /// Command to deactivate (terminate) a staff member through Provider aggregate
    /// Follows DDD principles by operating on Provider aggregate root
    /// </summary>
    public sealed record DeactivateProviderStaffCommand(
        Guid ProviderId,
        Guid StaffId,
        string Reason) : ICommand<DeactivateProviderStaffResult>
    {
        public Guid? IdempotencyKey { get; init; }
    }

    /// <summary>
    /// Result returned after successfully deactivating staff
    /// </summary>
    public sealed record DeactivateProviderStaffResult(
        Guid ProviderId,
        Guid StaffId,
        string FullName,
        bool IsActive,
        DateTime DeactivatedAt,
        string Reason);
}
