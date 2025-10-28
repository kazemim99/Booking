// ========================================
// Application/Commands/Provider/ActivateProviderStaff/ActivateProviderStaffCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProviderStaff
{
    /// <summary>
    /// Command to reactivate a deactivated staff member through Provider aggregate
    /// Follows DDD principles by operating on Provider aggregate root
    /// </summary>
    public sealed record ActivateProviderStaffCommand(
        Guid ProviderId,
        Guid StaffId) : ICommand<ActivateProviderStaffResult>
    {
        public Guid? IdempotencyKey { get; init; }
    }

    /// <summary>
    /// Result returned after successfully activating staff
    /// </summary>
    public sealed record ActivateProviderStaffResult(
        Guid ProviderId,
        Guid StaffId,
        string FullName,
        bool IsActive,
        DateTime ActivatedAt);
}
