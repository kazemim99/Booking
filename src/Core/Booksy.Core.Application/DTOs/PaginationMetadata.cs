// ========================================
// Booksy.Core.Application/DTOs/PaginationMetadata.cs
// ========================================
namespace Booksy.Core.Application.DTOs
{


    /// <summary>
    /// Pagination metadata information
    /// </summary>
    public sealed class PaginationMetadata
    {
        public int TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalPages { get; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public int? PreviousPage => HasPreviousPage ? PageNumber - 1 : null;
        public int? NextPage => HasNextPage ? PageNumber + 1 : null;

        public PaginationMetadata(int totalCount, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
            if (pageSize < 1) throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

            TotalCount = Math.Max(0, totalCount);
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        }
    }

}
