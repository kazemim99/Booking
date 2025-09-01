using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.ValueObjects;

/// <summary>
/// Represents a date range with start and end dates.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static DateRange Create(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new DomainValidationException("DateRange", "Start date must be before or equal to end date");

        return new DateRange(startDate, endDate);
    }

    public static DateRange CreateFromDuration(DateTime startDate, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new DomainValidationException("Duration", "Duration cannot be negative");

        return new DateRange(startDate, startDate.Add(duration));
    }

    public TimeSpan Duration => EndDate - StartDate;

    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;

    public bool Overlaps(DateRange other)
    {
        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }

    public bool IsWithin(DateRange other)
    {
        return StartDate >= other.StartDate && EndDate <= other.EndDate;
    }

    public DateRange? GetOverlap(DateRange other)
    {
        if (!Overlaps(other))
            return null;

        var overlapStart = StartDate > other.StartDate ? StartDate : other.StartDate;
        var overlapEnd = EndDate < other.EndDate ? EndDate : other.EndDate;

        return Create(overlapStart, overlapEnd);
    }

    public DateRange Extend(TimeSpan extension)
    {
        return Create(StartDate, EndDate.Add(extension));
    }

    public DateRange Shift(TimeSpan offset)
    {
        return Create(StartDate.Add(offset), EndDate.Add(offset));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}