// ========================================
// Application/Commands/Provider/UpdateProviderStaff/UpdateProviderStaffCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderStaff
{
    /// <summary>
    /// Command to update an existing staff member through Provider aggregate
    /// Follows DDD principles by operating on Provider aggregate root
    /// </summary>
    public sealed record UpdateProviderStaffCommand(
        Guid ProviderId,
        Guid StaffId,
        string? FirstName,
        string? LastName,
        string? Email,
        string? PhoneNumber,
        string? CountryCode,
        string? Role,
        string? Notes = null,
        string? Biography = null,
        string? ProfilePhotoUrl = null) : ICommand<UpdateProviderStaffResult>
    {
        public Guid? IdempotencyKey { get; init; }
    }

    /// <summary>
    /// Result returned after successfully updating staff
    /// </summary>
    public sealed record UpdateProviderStaffResult(
        Guid ProviderId,
        Guid StaffId,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        bool IsActive,
        DateTime UpdatedAt);
}
