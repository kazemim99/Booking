// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaff
{
    public sealed record AddStaffCommand(
        Guid ProviderId,
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        StaffRole Role,
        string? Notes = null,
        Guid? IdempotencyKey = null) : ICommand<AddStaffResult>;
}