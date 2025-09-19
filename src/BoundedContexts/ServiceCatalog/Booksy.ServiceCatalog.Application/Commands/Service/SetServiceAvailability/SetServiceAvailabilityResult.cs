// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/SetServiceAvailability/SetServiceAvailabilityResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Service.SetServiceAvailability
{
    public sealed record SetServiceAvailabilityResult(
        Guid ServiceId,
        DateTime UpdatedAt,
        int AvailabilityCount);
}