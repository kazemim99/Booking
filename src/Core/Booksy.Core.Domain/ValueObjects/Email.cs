using System.Text.RegularExpressions;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents a valid email address
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets the email address value
    /// </summary>
    public string Value { get; }
    public static Email From(string value) => new(value);

    /// <summary>
    /// Initializes a new instance of the Email class
    /// </summary>
    /// <param name="value">The email address</param>
    /// <exception cref="ArgumentException">Thrown when email format is invalid</exception>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(Email), "Email is required");

        if (!IsValidEmail(value))
            throw new DomainValidationException(nameof(Email), "Invalid email format");

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);

    public override string ToString() => Value;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}



public class EmailValueConverter : ValueConverter<Email, string>
{
    public EmailValueConverter()
        : base(
            email => email.Value.ToLowerInvariant(),  // Email -> string
            value => new Email(value))                // string -> Email
    {
    }
}
