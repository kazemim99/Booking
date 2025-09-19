// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessHours.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Business hours for a specific day of the week
    /// </summary>
    public sealed class BusinessHours : Entity<Guid>
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public OperatingHours? OperatingHours { get; private set; }
        public bool IsOpen { get; private set; } = true;

        // Private constructor for EF Core
        private BusinessHours() : base() { }

        internal static BusinessHours Create(DayOfWeek dayOfWeek, TimeOnly openTime, TimeOnly closeTime)
        {
            return new BusinessHours
            {
                Id = Guid.NewGuid(),
                DayOfWeek = dayOfWeek,
                OperatingHours = ValueObjects.OperatingHours.Create(openTime, closeTime)
            };
        }

        internal static BusinessHours CreateClosed(DayOfWeek dayOfWeek)
        {
            return new BusinessHours
            {
                Id = Guid.NewGuid(),
                DayOfWeek = dayOfWeek,
                OperatingHours = null
            };
        }

        public void UpdateHours(TimeOnly openTime, TimeOnly closeTime)
        {
            OperatingHours = ValueObjects.OperatingHours.Create(openTime, closeTime);
        }

        public void SetClosed()
        {
            OperatingHours = null;
        }
    }
}