// ========================================
// Booksy.ServiceCatalog.Api/Models/Responses/ProviderStatusResponse.cs
// ========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    /// <summary>
    /// Response model for Provider status endpoint
    /// </summary>
    public sealed class ProviderStatusResponse
    {
        /// <summary>
        /// Provider ID
        /// </summary>
        public Guid ProviderId { get; set; }

        /// <summary>
        /// Current Provider status (Drafted, PendingVerification, Verified, Active, Inactive, Suspended, Archived)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// User ID (Owner ID)
        /// </summary>
        public Guid UserId { get; set; }
    }
}
