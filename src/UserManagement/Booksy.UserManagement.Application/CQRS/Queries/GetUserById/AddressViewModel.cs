
namespace Booksy.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed class AddressViewModel
    {
        public string Street { get; init; } = string.Empty;
        public string? Unit { get; init; }
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string FullAddress { get; init; } = string.Empty;
    }
}

