// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RegisterCustomer/RegisterCustomerCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RegisterCustomer
{
    /// <summary>
    /// Command to register a new customer
    /// </summary>
    public sealed record RegisterCustomerCommand : ICommand<RegisterCustomerResult>
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public AddressDto? Address { get; init; }
        public string? Bio { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
    }

    public sealed record AddressDto(
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        string? Unit = null);
}
