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
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
