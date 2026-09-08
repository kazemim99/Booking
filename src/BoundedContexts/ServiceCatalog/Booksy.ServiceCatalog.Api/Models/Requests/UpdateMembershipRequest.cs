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
public sealed record UpdateMembershipRequest(
    string? DisplayName = null,
    string? BioOverride = null,
    bool? ProvidesServices = null,
    string? PhotoUrl = null);
