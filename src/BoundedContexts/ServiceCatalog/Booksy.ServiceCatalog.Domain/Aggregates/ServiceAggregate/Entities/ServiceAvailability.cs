// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Entities/ServiceAvailability.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Service availability configuration within Service aggregate
    /// </summary>
    public sealed class ServiceAvailability : Entity<Guid>
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public OperatingHours? AvailableHours { get; private set; }
        public bool IsAvailable { get; private set; }
        public int MaxConcurrentBookings { get; private set; }
        public List<Guid> AvailableStaff { get; private set; } = new();
        public DateTime EffectiveFrom { get; private set; }
        public DateTime? EffectiveTo { get; private set; }

        // Private constructor for EF Core
        private ServiceAvailability() : base() { }

        internal static ServiceAvailability Create(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, int maxConcurrent = 1)
        {
            return new ServiceAvailability
            {
                Id = Guid.NewGuid(),
                DayOfWeek = dayOfWeek,
                AvailableHours = OperatingHours.Create(startTime, endTime),
                IsAvailable = true,
                MaxConcurrentBookings = maxConcurrent,
                EffectiveFrom = DateTime.UtcNow
            };
        }

        internal static ServiceAvailability CreateUnavailable(DayOfWeek dayOfWeek)
        {
            return new ServiceAvailability
            {
                Id = Guid.NewGuid(),
                DayOfWeek = dayOfWeek,
                AvailableHours = null,
                IsAvailable = false,
                MaxConcurrentBookings = 0,
                EffectiveFrom = DateTime.UtcNow
            };
        }

        public void UpdateAvailableHours(TimeOnly startTime, TimeOnly endTime)
        {
            AvailableHours = OperatingHours.Create(startTime, endTime);
            IsAvailable = true;
        }

        public void SetUnavailable()
        {
            AvailableHours = null;
            IsAvailable = false;
            AvailableStaff.Clear();
        }

        public void SetMaxConcurrentBookings(int maxConcurrent)
        {
            if (maxConcurrent < 0)
                throw new ArgumentException("Max concurrent bookings cannot be negative", nameof(maxConcurrent));

            MaxConcurrentBookings = maxConcurrent;
        }

        public void AddAvailableStaff(Guid staffId)
        {
            if (!AvailableStaff.Contains(staffId))
            {
                AvailableStaff.Add(staffId);
            }
        }

        public void RemoveAvailableStaff(Guid staffId)
        {
            AvailableStaff.Remove(staffId);
        }

        public bool IsStaffAvailable(Guid staffId)
        {
            return AvailableStaff.Contains(staffId);
        }

        public bool IsAvailableAt(TimeOnly time)
        {
            if (!IsAvailable || AvailableHours == null)
                return false;

            return time >= AvailableHours.StartTime && time <= AvailableHours.EndTime;
        }

        public void SetEffectivePeriod(DateTime from, DateTime? to = null)
        {
            EffectiveFrom = from;
            EffectiveTo = to;
        }
    }
}