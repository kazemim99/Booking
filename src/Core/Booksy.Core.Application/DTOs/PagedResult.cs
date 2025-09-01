//// ========================================
//// Booksy.Core.Application/DTOs/PagedResult.cs
//// ========================================
//namespace Booksy.Core.Application.DTOs
//{
//    /// <summary>
//    /// Represents a paged result set
//    /// </summary>
//    /// <typeparam name="T">The type of items in the result</typeparam>
//    public sealed class PagedResult<T>
//    {
//        public IReadOnlyList<T> Items { get; }
//        public int TotalCount { get; }
//        public int PageNumber { get; }
//        public int PageSize { get; }
//        public int TotalPages { get; }
//        public bool HasPreviousPage { get; }
//        public bool HasNextPage { get; }

//        public PagedResult(
//            IReadOnlyList<T> items,
//            int totalCount,
//            int pageNumber,
//            int pageSize)
//        {
//            Items = items ?? new List<T>();
//            TotalCount = totalCount;
//            PageNumber = pageNumber;
//            PageSize = pageSize;
//            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
//            HasPreviousPage = pageNumber > 1;
//            HasNextPage = pageNumber < TotalPages;
//        }

//        public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
//        {
//            return new PagedResult<T>(new List<T>(), 0, pageNumber, pageSize);
//        }

//        public PagedResult<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
//        {
//            var mappedItems = Items.Select(mapper).ToList();
//            return new PagedResult<TOutput>(mappedItems, TotalCount, PageNumber, PageSize);
//        }

//        public async Task<PagedResult<TOutput>> MapAsync<TOutput>(Func<T, Task<TOutput>> mapper)
//        {
//            var mappedItems = await Task.WhenAll(Items.Select(mapper));
//            return new PagedResult<TOutput>(mappedItems, TotalCount, PageNumber, PageSize);
//        }
//    }
//}