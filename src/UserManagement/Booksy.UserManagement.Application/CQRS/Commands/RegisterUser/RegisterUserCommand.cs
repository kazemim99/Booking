// ========================================
// Booksy.UserManagement.Application/Commands/RegisterUser/RegisterUserCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Commands.RegisterUser
{
    /// <summary>
    /// Command to register a new user
    /// </summary>
    public sealed record RegisterUserCommand : ICommand<RegisterUserResult>
    {
        public RegisterUserCommand(string email, string password, string firstName, string lastName, string? phoneNumber, string userType, bool acceptTerms, bool marketingConsent, string? referralCode)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            UserType1 = userType;
            AcceptTerms = acceptTerms;
            MarketingConsent = marketingConsent;
            ReferralCode = referralCode;
        }

        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? PhoneNumber { get; init; }
        public UserType UserType { get; init; } = UserType.Customer;
        public AddressDto? Address { get; init; }
        public string? Bio { get; init; }
        public string? PreferredLanguage { get; init; }
        public string? TimeZone { get; init; }
        public string? ReferralCode { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public Guid? IdempotencyKey { get; init; }
        public bool SendWelcomeEmail { get; init; } = true;
        public bool RequireEmailVerification { get; init; } = true;
        public Dictionary<string, string>? Preferences { get; init; }
        public string UserType1 { get; }
        public bool AcceptTerms { get; }
        public bool MarketingConsent { get; }
    }

    public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? Unit = null);
}

