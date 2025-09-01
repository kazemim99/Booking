namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Supported currencies in the system
/// </summary>
public enum Currency
{
    /// <summary>
    /// US Dollar
    /// </summary>
    USD = 1,

    /// <summary>
    /// Euro
    /// </summary>
    EUR = 2,

    /// <summary>
    /// British Pound
    /// </summary>
    GBP = 3,

    /// <summary>
    /// Canadian Dollar
    /// </summary>
    CAD = 4,

    /// <summary>
    /// Australian Dollar
    /// </summary>
    AUD = 5
}