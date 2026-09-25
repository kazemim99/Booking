// ========================================
// AsanRezerve.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Exceptions
{

    public sealed class ServiceNotAvailableException : ServiceCatalogDomainException
    {
        public ServiceNotAvailableException(ServiceId serviceId)
            : base($"Service {serviceId} is not available for booking") { }

        public override string ErrorCode => "SERVICE_NOT_AVAILABLE";
    }
}