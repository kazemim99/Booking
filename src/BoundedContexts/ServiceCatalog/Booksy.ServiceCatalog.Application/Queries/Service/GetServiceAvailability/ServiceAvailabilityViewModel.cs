
namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceAvailability
{
    public sealed class ServiceAvailabilityViewModel
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int ServiceDuration { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? StaffId { get; set; }
        public List<AvailabilitySlotViewModel> AvailableSlots { get; set; } = new();
        public int TotalSlots { get; set; }
        public bool IsBookable { get; set; }
    }
}