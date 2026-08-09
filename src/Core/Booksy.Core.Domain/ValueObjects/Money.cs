using Booksy.Core.Domain.Base;

using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainValidationException("Amount", "Money amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainValidationException("Currency", "Currency code is required");

        if (currency.Length != 3)
            throw new DomainValidationException("Currency", "Currency code must be a 3-letter ISO code");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => Create(0, currency);

    /// <summary>
    /// Returns a reference-distinct copy carrying the same value.
    /// <para>
    /// Required because <see cref="Money"/> is persisted as an EF Core <b>owned</b> entity: two owned
    /// navigations on the same aggregate (e.g. <c>Payment.Amount</c> and <c>Payment.PaidAmount</c>, or a
    /// child <c>Transaction.Amount</c>) must never share the same CLR instance. Sharing an instance corrupts
    /// the EF change tracker (raises <c>DuplicateDependentEntityTypeInstanceWarning</c>) and causes a
    /// newly-added child to be tracked as <c>Modified</c> instead of <c>Added</c> — producing a phantom
    /// UPDATE that affects 0 rows and throws <c>DbUpdateConcurrencyException</c>. Assign a <see cref="Clone"/>
    /// whenever the same monetary value flows into more than one owned slot.
    /// </para>
    /// </summary>
    public new Money Clone() => new(Amount, Currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainValidationException("Currency", $"Cannot add money with different currencies: {Currency} and {other.Currency}");

        return Create(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainValidationException("Currency", $"Cannot subtract money with different currencies: {Currency} and {other.Currency}");

        return Create(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return Create(Amount * factor, Currency);
    }

    public bool IsZero() => Amount == 0;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}