namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Defines the hierarchical type of a provider in the system.
    /// This is separate from ProviderType which categorizes the business type (Salon, Clinic, etc.)
    /// </summary>
    public enum ProviderHierarchyType
    {
        /// <summary>
        /// Organization provider - represents a business entity (salon, clinic, etc.)
        /// Can have staff members (Individual providers) working under it
        /// </summary>
        Organization = 0,

        /// <summary>
        /// Individual provider - represents a single professional
        /// Can be independent (solo) or linked to an Organization
        /// </summary>
        Individual = 1
    }
}
