

namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    public sealed class ServiceGetByIdSpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public ServiceGetByIdSpecification(
            ServiceId Id)
        {
            // Base criteria - only active services for search

            AddCriteria(service => service.Id == Id);
            AddInclude(c => c.Provider);

        }
    }

}