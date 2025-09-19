// ========================================
// Booksy.UserManagement.Domain/Specifications/SearchUsersCountSpecification.cs
// ========================================
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Specifications;

/// <summary>
/// Specification for getting user search count (without pagination)
/// </summary>
public sealed class SearchUsersCountSpecification : BaseSpecification<User>
{
    public SearchUsersCountSpecification(
        string? searchTerm = null,
        UserStatus? status = null,
        UserType? type = null,
        string? role = null,
        string? city = null,
        string? country = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null,
        bool includeInactive = false)
    {
        // Apply the same filters as SearchUsersSpecification but without pagination and includes

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            AddCriteria(user =>
                user.Email.Value.ToLower().Contains(term) ||
                user.Profile.FirstName.ToLower().Contains(term) ||
                user.Profile.LastName.ToLower().Contains(term) ||
                (user.Profile.PhoneNumber != null && user.Profile.PhoneNumber.Value.Contains(term)));
        }

        if (status.HasValue)
        {
            AddCriteria(user => user.Status == status.Value);
        }
        else if (!includeInactive)
        {
            AddCriteria(user => user.Status == UserStatus.Active || user.Status == UserStatus.Pending);
        }

        if (type.HasValue)
        {
            AddCriteria(user => user.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            AddCriteria(user => user.Roles.Any(r =>
                r.Name == roleName &&
                (!r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityLower = city.Trim().ToLower();
            AddCriteria(user =>
                user.Profile.Address != null &&
                user.Profile.Address.City.ToLower().Contains(cityLower));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countryLower = country.Trim().ToLower();
            AddCriteria(user =>
                user.Profile.Address != null &&
                user.Profile.Address.Country.ToLower().Contains(countryLower));
        }

        if (registeredAfter.HasValue)
        {
            AddCriteria(user => user.RegisteredAt >= registeredAfter.Value);
        }

        if (registeredBefore.HasValue)
        {
            AddCriteria(user => user.RegisteredAt <= registeredBefore.Value);
        }
    }
}