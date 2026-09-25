// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.AddStaff
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