//===========================================
// Models/Responses/ProviderFullRegistrationResponse.cs
//===========================================

namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response for complete provider registration
/// </summary>
public sealed class ProviderFullRegistrationResponse
{
    /// <summary>
    /// Created provider ID
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Business name
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Provider status (Pending, Active, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Number of services registered
    /// </summary>
    public int ServicesCount { get; set; }

    /// <summary>
    /// Number of staff members added
    /// </summary>
    public int StaffCount { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
