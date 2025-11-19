// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateCustomerProfile/UpdateCustomerProfileCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Application.DTOs;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile
{
    /// <summary>
    /// Command to update customer profile information
    /// </summary>
    public sealed record UpdateCustomerProfileCommand : ICommand<UpdateCustomerProfileResult>
    {
        public Guid CustomerId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? PhoneNumber { get; init; }
        public AddressDto? Address { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }

        public Guid? IdempotencyKey { get; init; }
    }
}
