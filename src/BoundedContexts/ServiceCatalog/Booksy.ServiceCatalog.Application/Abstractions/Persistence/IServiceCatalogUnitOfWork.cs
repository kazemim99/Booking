using Booksy.Core.Application.Abstractions.Persistence;

namespace Booksy.ServiceCatalog.Application.Abstractions.Persistence;

/// <summary>
/// ServiceCatalog-scoped Unit of Work. In the modular monolith both bounded contexts
/// live in one DI container; injecting this marker (instead of the shared
/// <see cref="IUnitOfWork"/>) guarantees handlers commit against the
/// ServiceCatalog DbContext rather than whichever context registered last.
/// </summary>
public interface IServiceCatalogUnitOfWork : IUnitOfWork
{
}
