// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public abstract class ServiceCatalogDomainException : DomainException
    {
        protected ServiceCatalogDomainException(string message) : base(message) { }
        protected ServiceCatalogDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}