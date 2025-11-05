// ========================================
// Application/Commands/Provider/AddStaffToProvider/AddStaffToProviderCommand.cs
// DDD-Compliant: Operates on Provider aggregate
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;

public sealed record AddStaffToProviderCommand(
    Guid ProviderId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? CountryCode,
    string Role,
    string? Notes = null,
    string? Biography = null,
    string? ProfilePhotoUrl = null) : ICommand<AddStaffToProviderResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record AddStaffToProviderResult(
    Guid ProviderId,
    Guid StaffId,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime AddedAt);
