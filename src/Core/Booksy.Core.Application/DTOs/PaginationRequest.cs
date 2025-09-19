// Booksy.Core.Application/Pagination/PaginationRequest.cs
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Booksy.Core.Application.DTOs;

/// <summary>
/// Enhanced pagination request with better model binding support
/// </summary>
public  class PaginationRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (starts from 1)
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [FromQuery(Name = "size")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Sorting field and direction (e.g., "name:asc", "date:desc")
    /// </summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; init; } = "RegisteredAt";


    /// <summary>
    /// Sort in descending order
    /// </summary>
    [FromQuery(Name = "sortDesc")]
    public bool SortDescending { get; set; } = true;


    /// <summary>
    /// Convert sort string to SortingDescriptor list
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public List<SortingDescriptor> SortBy =>
        ParseSortString(Sort);

    /// <summary>
    /// Calculate skip count for database queries
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Take count for database queries
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Take => PageSize;

    /// <summary>
    /// Default pagination request
    /// </summary>
    public static PaginationRequest Default => new();

    /// <summary>
    /// Create pagination request with basic parameters
    /// </summary>
    public static PaginationRequest Create(int pageNumber, int pageSize, string? sort = null) =>
        new() { PageNumber = pageNumber, PageSize = pageSize, Sort = sort };

    /// <summary>
    /// Parse sort string like "name:asc,date:desc" into SortingDescriptor list
    /// </summary>
    private  List<SortingDescriptor> ParseSortString(string? sortString)
    {
        if (string.IsNullOrWhiteSpace(sortString))
            return new List<SortingDescriptor>();

        return sortString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(c=>ParseSingleSort(c))
            .Where(s => s != null)
            .Cast<SortingDescriptor>()
            .ToList();
    }

    private  SortingDescriptor? ParseSingleSort(string sortPart)
    {
        var parts = sortPart.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            return null;

        var fieldName = parts[0].Trim();
        var direction = SortDescending
            ? SortDirection.Descending
            : SortDirection.Ascending;

        return new SortingDescriptor { FieldName = fieldName, Direction = direction };
    }
}


/// <summary>
/// Sorting descriptor for pagination
/// </summary>
public sealed record SortingDescriptor
{
    public string FieldName { get; init; } = string.Empty;
    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}

/// <summary>
/// Sort direction enumeration
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}