
namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceAvailability
{

    public sealed class AvailabilitySlotViewModel
    {
        public Guid SlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
    }
}