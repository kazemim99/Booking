using AsanRezerve.Core.Domain.Exceptions;
using DomainDayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities
{
    /// <summary>
    /// One day of a member's working week AT THIS SALON.
    /// </summary>
    /// <remarks>
    /// Owned by the membership's <see cref="StaffProfile"/>, not by the person and not by
    /// the salon — which is exactly what makes "Tue–Thu here, Fri–Sat there" expressible:
    /// a person working at two salons has two memberships and therefore two schedules,
    /// while remaining one identity.
    ///
    /// <para>A member with NO working days falls back to the salon's own business hours.
    /// That keeps the common case (everyone works the shop's hours) free of data, and it
    /// is what every member did before per-member schedules existed.</para>
    /// </remarks>
    public sealed class StaffWorkingDay
    {
        public DomainDayOfWeek DayOfWeek { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }

        private StaffWorkingDay() { }

        public static StaffWorkingDay Create(DomainDayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            if (endTime <= startTime)
                throw new DomainValidationException(
                    nameof(StaffWorkingDay),
                    "A working day must end after it starts.");

            return new StaffWorkingDay
            {
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            };
        }

        /// <summary>
        /// Narrows this day to the salon's opening hours. A member cannot be bookable while
        /// the salon is shut, so the effective window is the intersection; null means the
        /// two do not overlap at all and the member simply is not bookable that day.
        /// </summary>
        public (TimeOnly Start, TimeOnly End)? IntersectWith(TimeOnly openTime, TimeOnly closeTime)
        {
            var start = StartTime > openTime ? StartTime : openTime;
            var end = EndTime < closeTime ? EndTime : closeTime;
            return end > start ? (start, end) : null;
        }
    }
}
