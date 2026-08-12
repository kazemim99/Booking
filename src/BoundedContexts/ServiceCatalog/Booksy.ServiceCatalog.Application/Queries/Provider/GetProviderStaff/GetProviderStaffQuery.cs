// ========================================
// Application/Queries/Provider/GetProviderStaff/GetProviderStaffQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff
{
    /// <summary>
    /// Query to get all staff members for a provider through Provider aggregate
    /// Follows DDD principles by querying through Provider aggregate root
    /// </summary>
    public sealed record GetProviderStaffQuery(
        Guid ProviderId,
        bool? IncludeInactive = false) : IQuery<GetProviderStaffResult>;

    /// <summary>
    /// Result containing the list of staff members
    /// </summary>
    public sealed record GetProviderStaffResult(
        Guid ProviderId,
        string ProviderName,
        List<StaffDto> Staff);

    /// <summary>
    /// DTO for staff member information
    /// </summary>
    public sealed record StaffDto(
        Guid Id,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string Role,
        bool IsActive,
        DateTime HiredAt,
        DateTime? TerminatedAt,
        string? Notes)
    {
        public string Biography { get; set; }
        public string ProfilePhotoUrl { get; set; }

        /// <summary>
        /// Whether this member actually performs services, i.e. can be booked.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="IsActive"/>: a receptionist or an owner who only runs the
        /// business is an active member but not a bookable resource, and asking for their
        /// availability correctly returns no slots. Management views list everyone; the
        /// customer-facing picker must offer only the bookable ones, or choosing a colleague
        /// leads to an empty calendar. Owners are not excluded by role — they opt in through
        /// SetOwnerProvidesServices — so this flag, not the role, is the test.
        /// </remarks>
        public bool ProvidesServices { get; set; }
    }
}
