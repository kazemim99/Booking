namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// How a required deposit is calculated. Both forms are part of the accepted product requirements
    /// ("allows setting deposit as percentage or fixed amount").
    /// </summary>
    public enum DepositType
    {
        /// <summary>A share of the booking total, expressed as a percentage (0–100).</summary>
        Percentage = 0,

        /// <summary>A flat amount in the booking's currency, independent of the total.</summary>
        FixedAmount = 1
    }
}
