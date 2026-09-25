// AsanRezerve.ServiceCatalog.Infrastructure/Persistence/ServiceCatalogUnitOfWork.cs
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

public class ServiceCatalogUnitOfWork : EfCoreUnitOfWork<ServiceCatalogDbContext>, IServiceCatalogUnitOfWork
{
    public ServiceCatalogUnitOfWork(ServiceCatalogDbContext context, ILogger<EfCoreUnitOfWork<ServiceCatalogDbContext>> logger, IDomainEventDispatcher eventDispatcher)
        : base(context, logger, eventDispatcher)
    {
    }
}
