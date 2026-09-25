using AsanRezerve.ServiceCatalog.Domain.Aggregates;

namespace AsanRezerve.ServiceCatalog.Domain.Specifications
{
    public sealed class ServicesByStatusSpecification : BaseSpecification<Service>
    {
        public ServicesByStatusSpecification(ServiceStatus status)
        {
            AddCriteria(service => service.Status == status);
            AddOrderBy(service => service.Name);
        }
    }
}

