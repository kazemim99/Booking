//===========================================
// Models/Responses/QualifiedStaffResponse.cs
//===========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    public sealed class QualifiedStaffResponse
    {
        public Guid ProviderId { get; set; }
        public Guid ServiceId { get; set; }
        public List<QualifiedStaffMemberResponse> QualifiedStaff { get; set; } = new();
    }

    public sealed class QualifiedStaffMemberResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public string? Specialization { get; set; }
    }
}
