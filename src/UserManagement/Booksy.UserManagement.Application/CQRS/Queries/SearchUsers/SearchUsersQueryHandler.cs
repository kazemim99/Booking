// ========================================
// Booksy.UserManagement.Application/Queries/SearchUsers/SearchUsersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Specifications;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.Queries.SearchUsers;

/// <summary>
/// Simplified handler for user search using specifications
/// </summary>
public sealed class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, PagedResult<SearchUsersResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SearchUsersQueryHandler> _logger;

    public SearchUsersQueryHandler(
        IUserRepository userRepository,
        ILogger<SearchUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<PagedResult<SearchUsersResult>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing user search with filters: {@Filters}", new
        {
            request.SearchTerm,
            request.Status,
            request.Type,
            request.Role,
            request.City,
            request.Country,
            request.Pagination.PageNumber,
            request.Pagination.PageSize
        });

        try
        {
            // Create business specification (no pagination logic!)
            var specification = new SearchUsersSpecification(
                searchTerm: request.SearchTerm,
                status: request.Status,
                type: request.Type,
                role: request.Role,
                city: request.City,
                country: request.Country,
                registeredAfter: request.RegisteredAfter,
                registeredBefore: request.RegisteredBefore,
                includeInactive: request.IncludeInactive,
                includeRoles: request.IncludeRoles,
                includeAddress: request.IncludeAddress);

            // Use generic pagination extension - One line handles everything!
            var result = await _userRepository
                .GetPaginatedAsync(specification, 
                request.Pagination,user=> new SearchUsersResult
                {
                    UserId = user.Id.Value,
                    Email = user.Email.Value,
                    FirstName = user.Profile.FirstName,
                    LastName = user.Profile.LastName,
                    PhoneNumber = user.Profile.PhoneNumber.Value,
                    Status = user.Status,
                    Type = user.Type,
                    IsLocked = user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    AvatarUrl = user.Profile.AvatarUrl,
                    City = user.Profile.Address.City,
                    Country = user.Profile.Address.Country,
                    RegisteredAt = user.RegisteredAt,
                    ActivatedAt = user.ActivatedAt,
                    LastLoginAt = user.LastLoginAt,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    Roles = request.IncludeRoles
                        ? user.Roles
                            .Where(r => !r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow)
                            .Select(r => new UserRoleInfo
                            {
                                Name = r.Name,
                                AssignedAt = r.AssignedAt,
                                ExpiresAt = r.ExpiresAt
                            }).ToList()
                        : new List<UserRoleInfo>(),
                    PrimaryRole = user.Roles
                        .Where(r => !r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow)
                        .OrderByDescending(r => r.AssignedAt)
                        .FirstOrDefault().Name
                });
            

            _logger.LogInformation("User search completed. Found {TotalCount} users, returning page {PageNumber} of {PageSize}",
                result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user search with query: {@Query}", request);
            throw;
        }
    }
}