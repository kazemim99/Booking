namespace Booksy.Core.Domain.Domain.Enums;

/// <summary>
/// Types of service locations
/// </summary>
public enum ServiceLocationType
{
    /// <summary>
    /// Service provided at provider's location only
    /// </summary>
    InHouse = 1,

    /// <summary>
    /// Service provided at client's location only
    /// </summary>
    Mobile = 2,

    /// <summary>
    /// Service can be provided at either location
    /// </summary>
    Both = 3
}