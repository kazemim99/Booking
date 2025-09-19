using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events;

/// <summary>
/// Domain event raised when a staff member is removed from a provider
/// </summary>
public sealed record StaffRemovedEvent : DomainEvent
{
    public ProviderId ProviderId { get; }
    public Guid StaffId { get; }
    public string StaffName { get; }
    public string StaffEmail { get; }
    public StaffRemovalReason RemovalReason { get; }
    public string? RemovalNotes { get; }
    public DateTime RemovalDate { get; }
    public string RemovedByUserId { get; }
    public IReadOnlyList<ServiceId> AffectedServices { get; }
    public IReadOnlyList<string> AffectedAppointmentIds { get; }
    public bool HasActiveAppointments { get; }
    public DateTime? LastWorkingDay { get; }
    public bool RequiresAppointmentReassignment { get; }

    public StaffRemovedEvent(
        ProviderId providerId,
        Guid? staffId,
        string staffName,
        string staffEmail,
        StaffRemovalReason removalReason,
        string removedByUserId,
        IReadOnlyList<ServiceId> affectedServices,
        IReadOnlyList<string> affectedAppointmentIds,
        string? removalNotes = null,
        DateTime? lastWorkingDay = null) : base()
    {
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        StaffId = staffId ?? throw new ArgumentNullException(nameof(staffId));
        StaffName = !string.IsNullOrWhiteSpace(staffName)
            ? staffName
            : throw new ArgumentException("Staff name cannot be null or empty", nameof(staffName));
        StaffEmail = !string.IsNullOrWhiteSpace(staffEmail)
            ? staffEmail
            : throw new ArgumentException("Staff email cannot be null or empty", nameof(staffEmail));
        RemovalReason = removalReason;
        RemovalNotes = removalNotes;
        RemovalDate = DateTime.UtcNow;
        RemovedByUserId = !string.IsNullOrWhiteSpace(removedByUserId)
            ? removedByUserId
            : throw new ArgumentException("Removed by user ID cannot be null or empty", nameof(removedByUserId));
        AffectedServices = affectedServices ?? Array.Empty<ServiceId>().ToList().AsReadOnly();
        AffectedAppointmentIds = affectedAppointmentIds ?? Array.Empty<string>().ToList().AsReadOnly();
        HasActiveAppointments = AffectedAppointmentIds.Any();
        LastWorkingDay = lastWorkingDay;
        RequiresAppointmentReassignment = HasActiveAppointments;
    }

    /// <summary>
    /// Get impact assessment of the staff removal
    /// </summary>
    public StaffRemovalImpact GetImpactAssessment()
    {
        var impactLevel = ImpactLevel.Low;

        if (HasActiveAppointments)
        {
            impactLevel = AffectedAppointmentIds.Count switch
            {
                > 10 => ImpactLevel.Critical,
                > 5 => ImpactLevel.High,
                > 0 => ImpactLevel.Medium,
                _ => ImpactLevel.Low
            };
        }
        else if (AffectedServices.Count > 3)
        {
            impactLevel = ImpactLevel.Medium;
        }

        return new StaffRemovalImpact
        {
            Level = impactLevel,
            AffectedServicesCount = AffectedServices.Count,
            AffectedAppointmentsCount = AffectedAppointmentIds.Count,
            RequiresImmediateAction = RequiresAppointmentReassignment,
            EstimatedResolutionTime = GetEstimatedResolutionTime()
        };
    }

    /// <summary>
    /// Check if removal requires specific notifications
    /// </summary>
    public bool RequiresClientNotification() => HasActiveAppointments;

    /// <summary>
    /// Check if removal requires management attention
    /// </summary>
    public bool RequiresManagementAttention() =>
        RemovalReason == StaffRemovalReason.Terminated ||
        RemovalReason == StaffRemovalReason.Disciplinary ||
        HasActiveAppointments;

    /// <summary>
    /// Get estimated time to resolve impacts
    /// </summary>
    private TimeSpan GetEstimatedResolutionTime()
    {
        if (!HasActiveAppointments) return TimeSpan.Zero;

        return AffectedAppointmentIds.Count switch
        {
            > 20 => TimeSpan.FromDays(3),
            > 10 => TimeSpan.FromDays(1),
            > 5 => TimeSpan.FromHours(8),
            > 0 => TimeSpan.FromHours(2),
            _ => TimeSpan.Zero
        };
    }

    /// <summary>
    /// Get required follow-up actions
    /// </summary>
    public IEnumerable<string> GetRequiredActions()
    {
        var actions = new List<string>();

        if (HasActiveAppointments)
        {
            actions.Add("Reassign or reschedule affected appointments");
            actions.Add("Notify affected clients about staff change");
        }

        if (AffectedServices.Any())
        {
            actions.Add("Update service staff assignments");
            actions.Add("Verify service availability after staff removal");
        }

        if (RemovalReason == StaffRemovalReason.Terminated)
        {
            actions.Add("Revoke system access and credentials");
            actions.Add("Complete exit interview documentation");
        }

        if (LastWorkingDay.HasValue && LastWorkingDay > DateTime.UtcNow)
        {
            actions.Add("Plan transition period activities");
            actions.Add("Schedule knowledge transfer sessions");
        }

        return actions;
    }
}

/// <summary>
/// Reasons for staff removal
/// </summary>
public enum StaffRemovalReason
{
    Resignation = 1,
    Terminated = 2,
    EndOfContract = 3,
    Retirement = 4,
    Relocation = 5,
    CareerChange = 6,
    Disciplinary = 7,
    Restructuring = 8,
    TemporaryLeave = 9,
    Other = 10
}

/// <summary>
/// Impact levels for staff removal
/// </summary>
public enum ImpactLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Impact assessment result
/// </summary>
public class StaffRemovalImpact
{
    public ImpactLevel Level { get; init; }
    public int AffectedServicesCount { get; init; }
    public int AffectedAppointmentsCount { get; init; }
    public bool RequiresImmediateAction { get; init; }
    public TimeSpan EstimatedResolutionTime { get; init; }
}