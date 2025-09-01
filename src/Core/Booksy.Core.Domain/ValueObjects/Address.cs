using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Domain.Utilities;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents a physical address
/// </summary>
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public string? Unit { get; }

    private Address(
        string street,
        string city,
        string state,
        string postalCode,
        string country,
        string? unit = null)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        Unit = unit;
    }

    public static Address Create(
        string street,
        string city,
        string state,
        string postalCode,
        string country,
        string? unit = null)
    {

        return new Address(
            Guard.Against.NullOrWhiteSpace(street, nameof(street)),
             Guard.Against.NullOrWhiteSpace(city, nameof(city)),
            Guard.Against.NullOrWhiteSpace(state, nameof(state)),
           Guard.Against.NullOrWhiteSpace(postalCode, nameof(postalCode)),
            Guard.Against.NullOrWhiteSpace(country, nameof(country)),
            unit);
    }

    public string GetFullAddress()
    {
        var fullAddress = Street;
        if (!string.IsNullOrEmpty(Unit))
            fullAddress = $"{Unit}, {fullAddress}";

        return $"{fullAddress}, {City}, {State} {PostalCode}, {Country}";
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
        yield return Unit;
    }

    public override string ToString() => GetFullAddress();
}