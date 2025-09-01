// ========================================
// Booksy.Core.Application/Abstractions/Persistence/IPaginatedRepository.cs
// ========================================
using System.Linq.Expressions;
using Booksy.Core.Application.DTOs;

namespace Booksy.Core.Application.Abstractions.Persistence;

/// <summary>
/// Generic interface for paginated queries
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
//public interface IPaginatedRepository<TEntity> where TEntity : class
//{
//    Task<PagedResult<TEntity>> GetPagedAsync(
//        int pageNumber,
//        int pageSize,
//        Expression<Func<TEntity, bool>>? filter = null,
//        Expression<Func<TEntity, object>>? orderBy = null,
//        bool descending = false,
//        CancellationToken cancellationToken = default);

//    Task<PagedResult<TProjection>> GetPagedAsync<TProjection>(
//        int pageNumber,
//        int pageSize,
//        Expression<Func<TEntity, TProjection>> projection,
//        Expression<Func<TEntity, bool>>? filter = null,
//        Expression<Func<TEntity, object>>? orderBy = null,
//        bool descending = false,
//        CancellationToken cancellationToken = default);
//}