// 📁 Booksy.UserManagement.Application/Abstractions/Queries/IUserQueryRepository.cs
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.UserManagement.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Application.Abstractions.Queries
{
    public interface IUserQueryRepository
    {
        // ✅ Application-specific query methods with DTOs
        Task<PagedResult<TResult>> GetPagedAsync<TResult>(
            IAdvancedSpecification<User> specification,
            Expression<Func<User, TResult>> selector,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TResult>> GetListAsync<TResult>(
            ISpecification<User> specification,
            Expression<Func<User, TResult>> selector,
            CancellationToken cancellationToken = default);

        Task<TResult?> GetSingleAsync<TResult>(
            ISpecification<User> specification,
            Expression<Func<User, TResult>> selector,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            ISpecification<User> specification,
            CancellationToken cancellationToken = default);
    }
}