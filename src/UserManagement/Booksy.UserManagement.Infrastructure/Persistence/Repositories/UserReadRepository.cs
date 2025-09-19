//// 📁 Booksy.UserManagement.Infrastructure/Persistence/Repositories/UserReadRepository.cs
//using Microsoft.EntityFrameworkCore;
//using Booksy.UserManagement.Domain.Repositories;
//using Booksy.UserManagement.Infrastructure.Persistence.Context;
//using Booksy.Core.Application.DTOs;
//using Booksy.Core.Domain.ValueObjects;
//using Booksy.UserManagement.Domain.ValueObjects;
//using System.Linq.Expressions;
//using Booksy.UserManagement.Domain.Enums;

//namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories
//{
//    public class UserReadRepository : IUserReadRepository
//    {
//        private readonly UserManagementDbContext _context;

//        public UserReadRepository(UserManagementDbContext context)
//        {
//            _context = context;
//        }

//        // ✅ NEW: Specification-based methods
//        public async Task<IReadOnlyList<User>> GetAsync(
//            ISpecification<User> specification,
//            CancellationToken cancellationToken = default)
//        {
//            return await SpecificationEvaluator<User>.ToListAsync(
//                _context.Users,
//                specification as IAdvancedSpecification<User> ?? throw new ArgumentException("Specification must implement IAdvancedSpecification"),
//                cancellationToken);
//        }

//        public async Task<User?> GetSingleAsync(
//            ISpecification<User> specification,
//            CancellationToken cancellationToken = default)
//        {
//            var query = SpecificationEvaluator<User>.GetQuery(
//                _context.Users,
//                specification as IAdvancedSpecification<User> ?? throw new ArgumentException("Specification must implement IAdvancedSpecification"));

//            return await query.FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<PagedResult<User>> GetPagedAsync(
//            IAdvancedSpecification<User> specification,
//            CancellationToken cancellationToken = default)
//        {
//            // Get total count without paging
//            var totalCount = await CountAsync(specification, cancellationToken);

//            // Get items with paging
//            var query = SpecificationEvaluator<User>.GetQuery(_context.Users, specification);
//            var items = await query.ToListAsync(cancellationToken);

//            var pageNumber = specification.IsPagingEnabled ? (specification.Skip / specification.Take) + 1 : 1;
//            var pageSize = specification.IsPagingEnabled ? specification.Take : totalCount;

//            return new PagedResult<User>(items, totalCount, pageNumber, pageSize);
//        }

//        public async Task<PagedResult<TResult>> GetPagedAsync<TResult>(
//            IAdvancedSpecification<User> specification,
//            Expression<Func<User, TResult>> selector,
//            CancellationToken cancellationToken = default)
//        {
//            // Count without paging and projection
//            var totalCount = await CountAsync(specification, cancellationToken);

//            // Apply specification and projection
//            var query = SpecificationEvaluator<User>.GetQuery(_context.Users, specification)
//                .Select(selector);

//            var items = await query.ToListAsync(cancellationToken);

//            var pageNumber = specification.IsPagingEnabled ? (specification.Skip / specification.Take) + 1 : 1;
//            var pageSize = specification.IsPagingEnabled ? specification.Take : totalCount;

//            return new PagedResult<TResult>(items, totalCount, pageNumber, pageSize);
//        }

//        public async Task<int> CountAsync(
//            ISpecification<User> specification,
//            CancellationToken cancellationToken = default)
//        {
//            return await SpecificationEvaluator<User>.CountAsync(_context.Users, specification, cancellationToken);
//        }

//        // ✅ Keep existing domain-specific methods
//        public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
//        {
//            return await _context.Users
//                .AsNoTracking()
//                .Include(u => u.Profile)
//                .Include(u => u.Roles)
//                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
//        }

//        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
//        {
//            return await _context.Users
//                .AsNoTracking()
//                .Include(u => u.Profile)
//                .Include(u => u.Roles)
//                .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
//        }

//        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IReadOnlyList<User>> GetByTypeAsync(UserType type, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<UserStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<User?> FindSingleAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<bool> ExistsAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken cancellationToken = default)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}