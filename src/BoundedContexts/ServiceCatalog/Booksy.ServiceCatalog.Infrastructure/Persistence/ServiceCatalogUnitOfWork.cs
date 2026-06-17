// Booksy.ServiceCatalog.Infrastructure/Persistence/ServiceCatalogUnitOfWork.cs
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

public class ServiceCatalogUnitOfWork : EfCoreUnitOfWork<ServiceCatalogDbContext>, IServiceCatalogUnitOfWork
{
    public ServiceCatalogUnitOfWork(ServiceCatalogDbContext context, ILogger<EfCoreUnitOfWork<ServiceCatalogDbContext>> logger, IDomainEventDispatcher eventDispatcher)
        : base(context, logger, eventDispatcher)
    {
    }
}
