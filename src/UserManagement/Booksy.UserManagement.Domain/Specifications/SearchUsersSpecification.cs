using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Domain.Specifications;

/// <summary>
/// Specification for comprehensive user search with filtering, sorting, and pagination
/// </summary>
public sealed class SearchUsersSpecification : BaseSpecification<User>
{
    public SearchUsersSpecification(
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
        bool includeAddress = false)
    {
        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            ApplySearchTermFilter(searchTerm.Trim());
        }

        // Apply status filter
        if (status.HasValue)
        {
            AddCriteria(user => user.Status == status.Value);
        }
        else if (!includeInactive)
        {
            // Exclude deleted and inactive users by default
            AddCriteria(user => user.Status == UserStatus.Active || user.Status == UserStatus.Pending);
        }

        // Apply type filter
        if (type.HasValue)
        {
            AddCriteria(user => user.Type == type.Value);
        }

        // Apply role filter
        if (!string.IsNullOrWhiteSpace(role))
        {
            ApplyRoleFilter(role.Trim());
        }

        // Apply location filters
        if (!string.IsNullOrWhiteSpace(city))
        {
            ApplyCityFilter(city.Trim());
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            ApplyCountryFilter(country.Trim());
        }

        // Apply date range filters
        if (registeredAfter.HasValue)
        {
            AddCriteria(user => user.RegisteredAt >= registeredAfter.Value);
        }

        if (registeredBefore.HasValue)
        {
            AddCriteria(user => user.RegisteredAt <= registeredBefore.Value);
        }

        // Add includes based on requirements
        AddInclude(u => u.Profile);

        if (includeRoles)
        {
            AddInclude(u => u.Roles.Where(r => !r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow));
        }

        if (includeAddress)
        {
            AddInclude("Profile.Address"); // String-based include for nested properties
        }

        // NO PAGINATION OR SORTING HERE - Handled by generic extensions!
    }

    private void ApplySearchTermFilter(string searchTerm)
    {
        var term = searchTerm.ToLower();
        AddCriteria(user =>
            user.Email.Value.ToLower().Contains(term) ||
            user.Profile.FirstName.ToLower().Contains(term) ||
            user.Profile.LastName.ToLower().Contains(term) ||
            (user.Profile.PhoneNumber != null && user.Profile.PhoneNumber.Value.Contains(term)));
    }

    private void ApplyRoleFilter(string role)
    {
        AddCriteria(user => user.Roles.Any(r =>
            r.Name == role &&
            (!r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow)));
    }

    private void ApplyCityFilter(string city)
    {
        var cityLower = city.ToLower();
        AddCriteria(user =>
            user.Profile.Address != null &&
            user.Profile.Address.City.ToLower().Contains(cityLower));
    }

    private void ApplyCountryFilter(string country)
    {
        var countryLower = country.ToLower();
        AddCriteria(user =>
            user.Profile.Address != null &&
            user.Profile.Address.Country.ToLower().Contains(countryLower));
    }
}


