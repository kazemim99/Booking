// Booksy.Core.Application/Pagination/PaginationRequest.cs
namespace Booksy.Core.Application.DTOs;

public record PaginationRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public List<SortingDescriptor> SortBy { get; set; } = new();
}

public class SortingDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending,
    Descending
}