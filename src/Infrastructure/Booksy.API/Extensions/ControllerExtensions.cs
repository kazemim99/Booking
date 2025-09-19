using Booksy.Core.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.API.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Create a paginated response with proper headers
    /// </summary>
    public static ActionResult<PagedResult<T>> PaginatedOk<T>(
        this ControllerBase controller,
        PagedResult<T> pagedResult)
    {
        // Add pagination metadata to response headers
        var metadata = pagedResult.GetMetadata();
        controller.Response.Headers.Add("X-Pagination", metadata.ToHeaderValue());

        // Add individual headers for easier access
        controller.Response.Headers.Add("X-Total-Count", pagedResult.TotalCount.ToString());
        controller.Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());
        controller.Response.Headers.Add("X-Current-Page", pagedResult.PageNumber.ToString());
        controller.Response.Headers.Add("X-Page-Size", pagedResult.PageSize.ToString());

        // Add Link header for navigation (RFC 5988)
        var links = new List<string>();
        var baseUrl = $"{controller.Request.Scheme}://{controller.Request.Host}{controller.Request.Path}";
        var queryParams = controller.Request.Query
            .Where(q => !q.Key.Equals("page", StringComparison.OrdinalIgnoreCase))
            .Select(q => $"{q.Key}={q.Value}")
            .ToList();

        if (pagedResult.HasPreviousPage)
        {
            var prevUrl = BuildUrl(baseUrl, queryParams, pagedResult.PreviousPageNumber!.Value);
            links.Add($"<{prevUrl}>; rel=\"prev\"");
        }

        if (pagedResult.HasNextPage)
        {
            var nextUrl = BuildUrl(baseUrl, queryParams, pagedResult.NextPageNumber!.Value);
            links.Add($"<{nextUrl}>; rel=\"next\"");
        }

        if (links.Any())
        {
            controller.Response.Headers.Add("Link", string.Join(", ", links));
        }

        return controller.Ok(pagedResult);
    }

    private static string BuildUrl(string baseUrl, List<string> queryParams, int pageNumber)
    {
        var allParams = new List<string>(queryParams) { $"page={pageNumber}" };
        return $"{baseUrl}?{string.Join("&", allParams)}";
    }
}

