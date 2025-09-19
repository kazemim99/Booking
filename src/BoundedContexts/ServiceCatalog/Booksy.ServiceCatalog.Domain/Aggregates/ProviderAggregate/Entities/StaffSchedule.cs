namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Staff member's individual schedule within Provider aggregate
    /// </summary>
    public sealed class StaffSchedule : Entity<Guid>
    {
        public Guid StaffId { get; private set; }
        public DayOfWeek DayOfWeek { get; private set; }
        public OperatingHours? WorkingHours { get; private set; }
        public List<OperatingHours> BreakTimes { get; private set; } = new();
        public bool IsAvailable { get; private set; }
        public DateTime EffectiveFrom { get; private set; }
        public DateTime? EffectiveTo { get; private set; }

        // Private constructor for EF Core
        private StaffSchedule() : base() { }

        internal static StaffSchedule Create(Guid staffId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateTime effectiveFrom)
        {
            return new StaffSchedule
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                DayOfWeek = dayOfWeek,
                WorkingHours = OperatingHours.Create(startTime, endTime),
                IsAvailable = true,
                EffectiveFrom = effectiveFrom
            };
        }

        internal static StaffSchedule CreateUnavailable(Guid staffId, DayOfWeek dayOfWeek, DateTime effectiveFrom)
        {
            return new StaffSchedule
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                DayOfWeek = dayOfWeek,
                WorkingHours = null,
                IsAvailable = false,
                EffectiveFrom = effectiveFrom
            };
        }

        public void UpdateWorkingHours(TimeOnly startTime, TimeOnly endTime)
        {
            WorkingHours = OperatingHours.Create(startTime, endTime);
            IsAvailable = true;
        }

        public void SetUnavailable()
        {
            WorkingHours = null;
            IsAvailable = false;
            BreakTimes.Clear();
        }

        public void AddBreak(TimeOnly startTime, TimeOnly endTime)
        {
            if (WorkingHours == null)
                throw new InvalidOperationException("Cannot add break to unavailable day");

            var breakTime = OperatingHours.Create(startTime, endTime);

            // Ensure break is within working hours
            if (breakTime.StartTime < WorkingHours.StartTime || breakTime.EndTime > WorkingHours.EndTime)
                throw new ArgumentException("Break time must be within working hours");

            BreakTimes.Add(breakTime);
        }

        public void RemoveBreak(TimeOnly startTime, TimeOnly endTime)
        {
            BreakTimes.RemoveAll(b => b.StartTime == startTime && b.EndTime == endTime);
        }

        public void ClearBreaks()
        {
            BreakTimes.Clear();
        }

        public bool IsWorkingAt(TimeOnly time)
        {
            if (!IsAvailable || WorkingHours == null)
                return false;

            if (time < WorkingHours.StartTime || time > WorkingHours.EndTime)
                return false;

            // Check if time falls within any break
            return !BreakTimes.Any(breakTime => time >= breakTime.StartTime && time <= breakTime.EndTime);
        }

        public void SetEffectivePeriod(DateTime from, DateTime? to = null)
        {
            EffectiveFrom = from;
            EffectiveTo = to;
        }
    }
}