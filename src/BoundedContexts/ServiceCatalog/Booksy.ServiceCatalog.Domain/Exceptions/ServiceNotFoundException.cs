// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public sealed class ServiceNotFoundException : ServiceCatalogDomainException
    {
        public ServiceNotFoundException(Guid serviceId) : base($"Service with id={serviceId} not found") { }

        public override string ErrorCode => throw new NotImplementedException();
    }
}