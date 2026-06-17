using Booksy.Core.Application.Abstractions.Persistence;

namespace Booksy.UserManagement.Application.Abstractions.Persistence;

/// <summary>
/// UserManagement-scoped Unit of Work. In the modular monolith both bounded contexts
/// live in one DI container; injecting this marker (instead of the shared
/// <see cref="IUnitOfWork"/>) guarantees handlers commit against the
/// UserManagement DbContext rather than whichever context registered last.
/// </summary>
public interface IUserManagementUnitOfWork : IUnitOfWork
{
}
