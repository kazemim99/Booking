// Booksy.Core.Application/DTOs/PagedResult.cs
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Booksy.Core.Application.DTOs;

public sealed class PagedResult<T>
{
    /// <summary>
    /// The items in this page
    /// </summary>
    public IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of items in the current page
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether this is the first page
    /// </summary>
    public bool IsFirstPage => PageNumber == 1;

    /// <summary>
    /// Whether this is the last page
    /// </summary>
    public bool IsLastPage => PageNumber == TotalPages;

    /// <summary>
    /// Previous page number (null if no previous page)
    /// </summary>
    public int? PreviousPageNumber => HasPreviousPage ? PageNumber - 1 : null;

    /// <summary>
    /// Next page number (null if no next page)
    /// </summary>
    public int? NextPageNumber => HasNextPage ? PageNumber + 1 : null;

    /// <summary>
    /// Range of item numbers on this page (e.g., "1-10 of 100")
    /// </summary>
    public string ItemRange => TotalCount == 0
        ? "0 of 0"
        : $"{((PageNumber - 1) * PageSize) + 1}-{Math.Min(PageNumber * PageSize, TotalCount)} of {TotalCount}";

    [JsonConstructor]
    public PagedResult(
        IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Items = items?.ToList().AsReadOnly() ?? new List<T>().AsReadOnly();
        TotalCount = Math.Max(0, totalCount);
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalPages = TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// Create an empty paginated result
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10) =>
        new(Array.Empty<T>(), 0, pageNumber, pageSize);

    /// <summary>
    /// Transform items to another type
    /// </summary>
    public PagedResult<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var mappedItems = Items.Select(mapper).ToList();
        return new PagedResult<TOutput>(mappedItems, TotalCount, PageNumber, PageSize);
    }

    /// <summary>
    /// Transform items to another type asynchronously
    /// </summary>
    public async Task<PagedResult<TOutput>> MapAsync<TOutput>(Func<T, Task<TOutput>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var mappedItems = await Task.WhenAll(Items.Select(mapper));
        return new PagedResult<TOutput>(mappedItems, TotalCount, PageNumber, PageSize);
    }

    /// <summary>
    /// Create a new paginated result with the same pagination info but different items
    /// </summary>
    public PagedResult<TOutput> WithItems<TOutput>(IEnumerable<TOutput> newItems) =>
        new(newItems, TotalCount, PageNumber, PageSize);

    /// <summary>
    /// Get pagination metadata as a separate object (useful for API headers)
    /// </summary>
    public PaginationMetadata GetMetadata() => new(
        PageNumber,
        PageSize,
        TotalPages,
        TotalCount,
        HasPreviousPage,
        HasNextPage);
}

/// <summary>
/// Pagination metadata that can be used in API headers
/// </summary>
public sealed record PaginationMetadata(
    int PageNumber,
    int PageSize,
    int TotalPages,
    int TotalCount,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public string ToHeaderValue() =>
        $"page={PageNumber},size={PageSize},total_pages={TotalPages},total_count={TotalCount}";
}
