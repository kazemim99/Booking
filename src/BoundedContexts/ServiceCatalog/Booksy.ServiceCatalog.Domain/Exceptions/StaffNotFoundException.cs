using Booksy.Core.Domain.Errors;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Exceptions;

/// <summary>
/// Exception thrown when a staff member cannot be found
/// </summary>
public sealed class StaffNotFoundException : DomainException
{
    public Guid? StaffId { get; }
    public ProviderId? ProviderId { get; }
    public string? SearchCriteria { get; }
    public StaffSearchContext SearchContext { get; }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException with staff ID
    /// </summary>
    public StaffNotFoundException(Guid staffId, ProviderId? providerId = null)
        : base($"Staff member with ID '{staffId}' was not found{(providerId != null ? $" for provider '{providerId}'" : "")}.")
    {
        StaffId = staffId;
        ProviderId = providerId;
        SearchContext = StaffSearchContext.ById;
        Data.Add("StaffId", staffId.ToString());
        if (providerId != null)
            Data.Add("ProviderId", providerId.ToString());
    }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException with search criteria
    /// </summary>
    public StaffNotFoundException(string searchCriteria, StaffSearchContext searchContext, ProviderId? providerId = null)
        : base(GenerateMessage(searchCriteria, searchContext, providerId))
    {
        SearchCriteria = searchCriteria;
        SearchContext = searchContext;
        ProviderId = providerId;
        Data.Add("SearchCriteria", searchCriteria);
        Data.Add("SearchContext", searchContext.ToString());
        if (providerId != null)
            Data.Add("ProviderId", providerId.ToString());
    }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException for provider context
    /// </summary>
    public StaffNotFoundException(ProviderId providerId, string reason = "No staff members found")
        : base($"No staff members found for provider '{providerId}'. {reason}")
    {
        ProviderId = providerId;
        SearchContext = StaffSearchContext.ByProvider;
        Data.Add("ProviderId", providerId.ToString());
        Data.Add("Reason", reason);
    }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException with inner exception
    /// </summary>
    public StaffNotFoundException(Guid staffId, Exception innerException)
        : base($"Staff member with ID '{staffId}' was not found.", innerException)
    {
        StaffId = staffId;
        SearchContext = StaffSearchContext.ById;
        Data.Add("StaffId", staffId.ToString());
    }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException with custom message
    /// </summary>
    public StaffNotFoundException(string message) : base(message)
    {
        SearchContext = StaffSearchContext.Custom;
    }

    /// <summary>
    /// Initializes a new instance of StaffNotFoundException with custom message and inner exception
    /// </summary>
    public StaffNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
        SearchContext = StaffSearchContext.Custom;
    }

    /// <summary>
    /// Creates exception for staff not found by email
    /// </summary>
    public static StaffNotFoundException ByEmail(string email, ProviderId? providerId = null)
    {
        return new StaffNotFoundException(email, StaffSearchContext.ByEmail, providerId);
    }

    /// <summary>
    /// Creates exception for staff not found by name
    /// </summary>
    public static StaffNotFoundException ByName(string name, ProviderId? providerId = null)
    {
        return new StaffNotFoundException(name, StaffSearchContext.ByName, providerId);
    }

    /// <summary>
    /// Creates exception for staff not found by role
    /// </summary>
    public static StaffNotFoundException ByRole(string role, ProviderId providerId)
    {
        return new StaffNotFoundException(role, StaffSearchContext.ByRole, providerId);
    }

    /// <summary>
    /// Creates exception for staff not found for specific service
    /// </summary>
    public static StaffNotFoundException ForService(ServiceId serviceId, ProviderId providerId)
    {
        var exception = new StaffNotFoundException($"No staff members found capable of providing service '{serviceId}' for provider '{providerId}'.");
        exception.Data.Add("ServiceId", serviceId.ToString());
        exception.Data.Add("ProviderId", providerId.ToString());
        return exception;
    }

    /// <summary>
    /// Creates exception for staff not available during specific time
    /// </summary>
    public static StaffNotFoundException ForTimeSlot(DateTime startTime, DateTime endTime, ProviderId providerId)
    {
        var exception = new StaffNotFoundException($"No staff members available from {startTime:yyyy-MM-dd HH:mm} to {endTime:yyyy-MM-dd HH:mm} for provider '{providerId}'.");
        exception.Data.Add("StartTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
        exception.Data.Add("EndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
        exception.Data.Add("ProviderId", providerId.ToString());
        return exception;
    }

    /// <summary>
    /// Creates exception for active staff not found
    /// </summary>
    public static StaffNotFoundException ActiveStaff(ProviderId providerId)
    {
        return new StaffNotFoundException(providerId, "All staff members are inactive or unavailable");
    }

    /// <summary>
    /// Creates exception for qualified staff not found
    /// </summary>
    public static StaffNotFoundException QualifiedStaff(string qualificationRequired, ProviderId providerId)
    {
        var exception = new StaffNotFoundException($"No staff members found with required qualification '{qualificationRequired}' for provider '{providerId}'.");
        exception.Data.Add("QualificationRequired", qualificationRequired);
        exception.Data.Add("ProviderId", providerId.ToString());
        return exception;
    }

    /// <summary>
    /// Get error code for this exception
    /// </summary>
    public override ErrorCode ErrorCode => ErrorCode.STAFF_NOT_FOUND;

    /// <summary>
    /// Get severity level for this exception
    /// </summary>
    public ExceptionSeverity Severity => SearchContext switch
    {
        StaffSearchContext.ById => ExceptionSeverity.High,
        StaffSearchContext.ByProvider => ExceptionSeverity.Medium,
        StaffSearchContext.ByService => ExceptionSeverity.Medium,
        StaffSearchContext.ByAvailability => ExceptionSeverity.Low,
        _ => ExceptionSeverity.Medium
    };

    /// <summary>
    /// Get suggested actions for resolving this exception
    /// </summary>
    public IEnumerable<string> GetSuggestedActions()
    {
        return SearchContext switch
        {
            StaffSearchContext.ById => new[]
            {
                "Verify the staff ID is correct",
                "Check if the staff member exists in the system",
                "Ensure the staff member belongs to the specified provider"
            },
            StaffSearchContext.ByProvider => new[]
            {
                "Add staff members to the provider",
                "Verify provider has active staff",
                "Check staff member status and availability"
            },
            StaffSearchContext.ByService => new[]
            {
                "Assign qualified staff to the service",
                "Train existing staff for the service",
                "Review service requirements and staff capabilities"
            },
            StaffSearchContext.ByAvailability => new[]
            {
                "Adjust the requested time slot",
                "Check staff schedules and working hours",
                "Consider alternative appointment times"
            },
            StaffSearchContext.ByRole => new[]
            {
                "Assign staff members to the required role",
                "Review role requirements",
                "Check staff member permissions and assignments"
            },
            StaffSearchContext.ByQualification => new[]
            {
                "Add qualified staff members",
                "Provide training to existing staff",
                "Review qualification requirements"
            },
            _ => new[]
            {
                "Review search criteria",
                "Check data consistency",
                "Verify system configuration"
            }
        };
    }

    /// <summary>
    /// Check if this exception is recoverable
    /// </summary>
    public bool IsRecoverable => SearchContext switch
    {
        StaffSearchContext.ById => false,
        StaffSearchContext.ByProvider => true,
        StaffSearchContext.ByService => true,
        StaffSearchContext.ByAvailability => true,
        StaffSearchContext.ByRole => true,
        StaffSearchContext.ByQualification => true,
        _ => false
    };

    /// <summary>
    /// Generate appropriate error message based on search context
    /// </summary>
    private static string GenerateMessage(string searchCriteria, StaffSearchContext searchContext, ProviderId? providerId)
    {
        var providerContext = providerId != null ? $" for provider '{providerId}'" : "";

        return searchContext switch
        {
            StaffSearchContext.ByEmail => $"Staff member with email '{searchCriteria}'{providerContext} was not found.",
            StaffSearchContext.ByName => $"Staff member with name '{searchCriteria}'{providerContext} was not found.",
            StaffSearchContext.ByRole => $"No staff members found with role '{searchCriteria}'{providerContext}.",
            StaffSearchContext.ByQualification => $"No staff members found with qualification '{searchCriteria}'{providerContext}.",
            _ => $"Staff member matching criteria '{searchCriteria}'{providerContext} was not found."
        };
    }
}

/// <summary>
/// Context in which staff search was performed
/// </summary>
public enum StaffSearchContext
{
    ById,
    ByEmail,
    ByName,
    ByRole,
    ByProvider,
    ByService,
    ByAvailability,
    ByQualification,
    Custom
}

/// <summary>
/// Exception severity levels
/// </summary>
public enum ExceptionSeverity
{
    Low,
    Medium,
    High,
    Critical
}