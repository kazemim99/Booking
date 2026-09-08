using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Organization-scoped edits to a membership. Every field is optional; a null field
/// means "leave unchanged" rather than "clear".
/// </summary>
/// <param name="DisplayName">
/// The salon's name for a member who has no app account. Rejected for a member who has
/// one — their name lives on their own Person record.
/// </param>
/// <param name="BioOverride">The member's bio at THIS salon.</param>
/// <param name="ProvidesServices">Whether the member currently takes customer bookings.</param>
/// <param name="PhotoUrl">Photo shown for the member at this salon.</param>
/// <param name="WorkingDays">
/// The days and hours this member works AT THIS SALON, narrowed to the salon's own
/// opening hours when availability is generated. An EMPTY list (as opposed to null)
/// puts them back on the salon's hours. Changing this regenerates their unbooked
/// future availability; booked slots are never touched.
/// </param>
/// <param name="ServiceIds">
/// Which of the salon's services this member performs. An EMPTY list (as opposed to
/// null) means they perform all of them.
/// </param>
public sealed record UpdateMembershipRequest(
    string? DisplayName = null,
    string? BioOverride = null,
    bool? ProvidesServices = null,
    string? PhotoUrl = null,
    IReadOnlyList<WorkingDayRequest>? WorkingDays = null,
    IReadOnlyList<Guid>? ServiceIds = null);

/// <summary>One day of a member's working week, e.g. Tuesday 10:00–16:00.</summary>
public sealed record WorkingDayRequest(
    DomainDayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
