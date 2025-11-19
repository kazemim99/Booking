using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Booksy.UserManagement.Domain.Enums;
using Booksy.Core.Application.DTOs;
using Booksy.UserManagement.Application.CQRS.Queries.SearchUsers;


namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Simplified request model for user search API endpoint
/// </summary>
public class SearchUsersRequest:PaginationRequest
{
    /// <summary>
    /// Search term - searches across email, first name, last name
    /// </summary>
    [FromQuery(Name = "q")]
    [StringLength(100, MinimumLength = 2)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by user status
    /// </summary>
    [FromQuery(Name = "status")]
    public UserStatus? Status { get; set; }

    /// <summary>
    /// Filter by user type
    /// </summary>
    [FromQuery(Name = "type")]
    public UserType? Type { get; set; }

    /// <summary>
    /// Filter users who have specific role
    /// </summary>
    [FromQuery(Name = "role")]
    [StringLength(50, MinimumLength = 2)]
    public string? Role { get; set; }

    /// <summary>
    /// Filter by city
    /// </summary>
    [FromQuery(Name = "city")]
    [StringLength(100, MinimumLength = 2)]
    public string? City { get; set; }

    /// <summary>
    /// Filter by country
    /// </summary>
    [FromQuery(Name = "country")]
    [StringLength(100, MinimumLength = 2)]
    public string? Country { get; set; }

    /// <summary>
    /// Filter users registered after this date (ISO format)
    /// </summary>
    [FromQuery(Name = "registeredAfter")]
    public DateTime? RegisteredAfter { get; set; }

    /// <summary>
    /// Filter users registered before this date (ISO format)
    /// </summary>
    [FromQuery(Name = "registeredBefore")]
    public DateTime? RegisteredBefore { get; set; }

    /// <summary>
    /// Include inactive users in results
    /// </summary>
    [FromQuery(Name = "includeInactive")]
    public bool IncludeInactive { get; set; } = false;

    

    /// <summary>
    /// Include user roles in results
    /// </summary>
    [FromQuery(Name = "includeRoles")]
    public bool IncludeRoles { get; set; } = false;

    /// <summary>
    /// Include user address in results
    /// </summary>
    [FromQuery(Name = "includeAddress")]
    public bool IncludeAddress { get; set; } = false;

    /// <summary>
    /// Converts this request to a SearchUsersQuery
    /// </summary>
    public SearchUsersQuery ToQuery()
    {
        return new SearchUsersQuery
        {
            SearchTerm = SearchTerm,
            Status = Status,
            Type = Type,
            Role = Role,
            City = City,
            Country = Country,
            RegisteredAfter = RegisteredAfter,
            RegisteredBefore = RegisteredBefore,
            IncludeInactive = IncludeInactive,
            IncludeRoles = IncludeRoles,
            IncludeAddress = IncludeAddress,
            Pagination = new PaginationRequest
            {
                PageNumber = PageNumber,
                PageSize = PageSize,
                Sort = Sort,
                SortDescending = SortDescending
            }
        };
    }

    /// <summary>
    /// Validates the request parameters
    /// </summary>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (RegisteredAfter.HasValue && RegisteredBefore.HasValue && RegisteredAfter > RegisteredBefore)
        {
            errors.Add("RegisteredAfter cannot be greater than RegisteredBefore");
        }

        //var validSortFields = new[] { "Email", "FirstName", "LastName", "RegisteredAt", "ActivatedAt", "LastLoginAt", "Status", "Type" };
        //if (!validSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
        //{
        //    errors.Add($"Invalid SortBy value. Valid options: {string.Join(", ", validSortFields)}");
        //}

        if (!string.IsNullOrWhiteSpace(SearchTerm) && SearchTerm.Length < 2)
        {
            errors.Add("SearchTerm must be at least 2 characters long");
        }

        return errors;
    }

    /// <summary>
    /// Gets a summary of active filters for logging
    /// </summary>
    public Dictionary<string, object?> GetActiveFilters()
    {
        var filters = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(SearchTerm)) filters["SearchTerm"] = SearchTerm;
        if (Status.HasValue) filters["Status"] = Status.ToString();
        if (Type.HasValue) filters["Type"] = Type.ToString();
        if (!string.IsNullOrWhiteSpace(Role)) filters["Role"] = Role;
        if (!string.IsNullOrWhiteSpace(City)) filters["City"] = City;
        if (!string.IsNullOrWhiteSpace(Country)) filters["Country"] = Country;
        if (RegisteredAfter.HasValue) filters["RegisteredAfter"] = RegisteredAfter;
        if (RegisteredBefore.HasValue) filters["RegisteredBefore"] = RegisteredBefore;
        if (IncludeInactive) filters["IncludeInactive"] = true;

        return filters;
    }
}