namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>How a customer entered a salon's customer book.</summary>
public enum CustomerSource
{
    /// <summary>Typed in by the provider.</summary>
    Manual = 0,

    /// <summary>Picked from the provider's phone contacts (only the ones they ticked).</summary>
    Contacts = 1,
}
