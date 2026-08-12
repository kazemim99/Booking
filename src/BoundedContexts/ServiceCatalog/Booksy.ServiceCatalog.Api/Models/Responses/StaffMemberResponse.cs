//===========================================
// Models/Responses/ProviderResponse.cs
//===========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    public sealed class StaffMemberResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The name to show a customer choosing who to book with.
        /// </summary>
        /// <remarks>
        /// Not derivable from <see cref="FirstName"/> + <see cref="LastName"/>: a member who
        /// was invited by phone but has not claimed their account yet has neither, and is
        /// identified solely by the salon-provided display name on their staff profile. While
        /// this field was missing, those members appeared in the picker as a blank row.
        /// </remarks>
        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
