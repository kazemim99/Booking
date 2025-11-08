// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerById/CustomerDetailsViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById
{
    public sealed class CustomerDetailsViewModel
    {
        public Guid CustomerId { get; init; }
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string FullName { get; init; } = string.Empty;
        public DateTime? DateOfBirth { get; init; }
        public int? Age { get; init; }
        public string? Gender { get; init; }
        public string? PhoneNumber { get; init; }
        public AddressViewModel? Address { get; init; }
        public string? AvatarUrl { get; init; }
        public string? Bio { get; init; }
        public bool IsActive { get; init; }
        public int FavoriteProvidersCount { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastModifiedAt { get; init; }
    }

    public sealed record AddressViewModel(
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        string? Unit = null);
}
