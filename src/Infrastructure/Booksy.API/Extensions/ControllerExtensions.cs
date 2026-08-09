using Booksy.Core.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booksy.API.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// The current user's id, taken from the claim the platform issues
    /// (<see cref="ClaimTypes.NameIdentifier"/>), falling back to <c>sub</c>.
    ///
    /// <para>Throws <see cref="UnauthorizedAccessException"/> — mapped to <b>401</b> by
    /// <c>ExceptionHandlingMiddleware</c> — when no usable id is present. Previously this called
    /// <c>Guid.Parse</c> directly, so a token missing the claim raised <see cref="ArgumentNullException"/> and
    /// surfaced as a <b>500</b>: a server-fault response to what is an authentication problem.</para>
    ///
    /// <para><b>Why this never returns <see cref="Guid.Empty"/>.</b> That would look tidier at the one call site
    /// that checks for it, but only one of this method's callers does; the rest would carry an empty id into
    /// queries and commands as though it were a real user. Failing loudly is the safe behaviour for an identity
    /// lookup, so an unusable claim is always an exception, never a sentinel.</para>
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">No parsable user-id claim on the principal.</exception>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "The authenticated principal carries no usable user-id claim.");
        }

        return id;
    }
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

