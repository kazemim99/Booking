//// ========================================
//// Booksy.Core.Application/DTOs/PagedResult.cs
//// ========================================
//using Booksy.Core.Application.DTOs.Booksy.Core.Application.DTOs.Booksy.Core.Application.DTOs;

//namespace Booksy.Core.Application.DTOs
//{


//    // ========================================
//    // Booksy.Core.Application/DTOs/PagedResult.cs
//    // ========================================
//    namespace Booksy.Core.Application.DTOs;

//    /// <summary>
//    /// Generic paged result wrapper
//    /// </summary>
//    /// <typeparam name="T">Type of items in the page</typeparam>
//    public sealed class PagedResult<T>
//    {
//        public IReadOnlyList<T> Items { get; }
//        public PaginationMetadata Metadata { get; }

//        public PagedResult(IReadOnlyList<T> items, PaginationMetadata metadata)
//        {
//            Items = items ?? throw new ArgumentNullException(nameof(items));
//            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
//        }

//        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
//            : this(items, new PaginationMetadata(totalCount, pageNumber, pageSize))
//        {
//        }
//    }

//}
