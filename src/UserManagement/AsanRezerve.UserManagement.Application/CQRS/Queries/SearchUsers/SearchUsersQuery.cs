// ========================================
// AsanRezerve.UserManagement.Application/Queries/SearchUsers/SearchUsersQuery.cs
// ========================================
using AsanRezerve.Core.Application.CQRS;
using AsanRezerve.UserManagement.Domain.Enums;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.SearchUsers;

/// <summary>
/// Simplified query for user search with essential filtering options
/// </summary>
public sealed record SearchUsersQuery : PaginatedQueryBase<SearchUsersResult>
{
    /// <summary>
    /// Search term - searches across email, first name, last name
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by user status
    /// </summary>
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Filter by user type
    /// </summary>
    public UserType? Type { get; init; }

    /// <summary>
    /// Filter users who have specific role
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Filter by city
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Filter by country
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Filter users registered after this date
    /// </summary>
    public DateTime? RegisteredAfter { get; init; }

    /// <summary>
    /// Filter users registered before this date
    /// </summary>
    public DateTime? RegisteredBefore { get; init; }

    /// <summary>
    /// Include inactive users in results
    /// </summary>
    public bool IncludeInactive { get; init; } = false;

    /// <summary>
    /// Include user roles in results
    /// </summary>
    public bool IncludeRoles { get; init; } = false;

    /// <summary>
    /// Include user address in results
    /// </summary>
    public bool IncludeAddress { get; init; } = false;

    // Not cacheable: an admin editing a user must see the edit in the next search, and the hand-built key this
    // cache used left out five filters, so different searches shared one entry (add-observability-and-caching).

    // Default constructor for model binding
    public SearchUsersQuery() : base() { }

    // Constructor with pagination
    public SearchUsersQuery(
        PaginationRequest pagination,
        string? searchTerm = null,
        UserStatus? status = null,
        UserType? type = null,
        string? role = null,
        string? city = null,
        string? country = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null,
        bool includeInactive = false,
        bool includeRoles = false,
        bool includeAddress = false) : base(pagination)
    {
        SearchTerm = searchTerm;
        Status = status;
        Type = type;
        Role = role;
        City = city;
        Country = country;
        RegisteredAfter = registeredAfter;
        RegisteredBefore = registeredBefore;
        IncludeInactive = includeInactive;
        IncludeRoles = includeRoles;
        IncludeAddress = includeAddress;
    }
}
