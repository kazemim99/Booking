namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Paginated response for booking list
/// </summary>
public class PaginatedBookingsResponse
{
    public IReadOnlyList<BookingResponse> Items { get; set; } = new List<BookingResponse>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
