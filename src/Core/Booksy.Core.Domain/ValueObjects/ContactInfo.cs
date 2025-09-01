using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Domain.ValueObjects;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents contact information including email and optional phone number
/// </summary>
public sealed class ContactInfo : ValueObject
{
    /// <summary>
    /// Gets the email address (required)
    /// </summary>
    public Email Email { get; }

    /// <summary>
    /// Gets the phone number (optional)
    /// </summary>
    public PhoneNumber? Phone { get; }

    /// <summary>
    /// Initializes a new instance of the ContactInfo class
    /// </summary>
    /// <param name="email">The email address (required)</param>
    /// <param name="phone">The phone number (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    public ContactInfo(Email email, PhoneNumber? phone = null)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Phone = phone;
    }

    /// <summary>
    /// Gets a formatted display string of the contact information
    /// </summary>
    /// <returns>Formatted contact information</returns>
    public string GetDisplayString()
    {
        if (Phone != null)
        {
            return $"Email: {Email}, Phone: {Phone}";
        }

        return $"Email: {Email}";
    }

    /// <summary>
    /// Gets a compact display string of the contact information
    /// </summary>
    /// <returns>Compact contact information</returns>
    public string GetCompactDisplay()
    {
        if (Phone != null)
        {
            return $"{Email} | {Phone}";
        }

        return Email.Value;
    }

    /// <summary>
    /// Returns the formatted contact information string
    /// </summary>
    /// <returns>The complete contact information display</returns>
    public override string ToString()
    {
        return GetDisplayString(); // 🎯 Key implementation!
    }

    /// <summary>
    /// Checks if phone number is available
    /// </summary>
    /// <returns>True if phone number is present, false otherwise</returns>
    public bool HasPhone() => Phone != null;

    /// <summary>
    /// Gets the primary contact method (always email)
    /// </summary>
    /// <returns>The email address</returns>
    public string GetPrimaryContact() => Email.Value;

    /// <summary>
    /// Gets the secondary contact method if available
    /// </summary>
    /// <returns>The phone number display string or null</returns>
    public string? GetSecondaryContact() => Phone?.ToString();

    /// <summary>
    /// Gets the atomic values for equality comparison
    /// </summary>
    /// <returns>Email and phone (if present) for equality</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Email;
        if (Phone != null) yield return Phone;
    }
}