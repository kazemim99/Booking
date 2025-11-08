// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using Booksy.Core.Domain.Errors;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{

    public sealed class ServiceNotAvailableException : ServiceCatalogDomainException
    {
        public ServiceNotAvailableException(ServiceId serviceId)
            : base($"Service {serviceId} is not available for booking") { }

        public override ErrorCode ErrorCode => ErrorCode.SERVICE_NOT_AVAILABLE;
    }
}