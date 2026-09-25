// ========================================
// AsanRezerve.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using AsanRezerve.Core.Domain.Exceptions;

namespace AsanRezerve.ServiceCatalog.Domain.Exceptions
{
    public abstract class ServiceCatalogDomainException : DomainException
    {
        protected ServiceCatalogDomainException(string message) : base(message) { }
        protected ServiceCatalogDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}