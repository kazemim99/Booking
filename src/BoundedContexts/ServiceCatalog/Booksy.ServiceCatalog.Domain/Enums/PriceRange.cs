namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the price tier/range for a provider's services
    /// Used for filtering providers by affordability
    /// </summary>
    public enum PriceRange
    {
        /// <summary>
        /// Budget-friendly pricing (lower cost services)
        /// </summary>
        Budget = 1,

        /// <summary>
        /// Moderate/mid-range pricing (average market rates)
        /// </summary>
        Moderate = 2,

        /// <summary>
        /// Premium pricing (high-end/luxury services)
        /// </summary>
        Premium = 3
    }
}
