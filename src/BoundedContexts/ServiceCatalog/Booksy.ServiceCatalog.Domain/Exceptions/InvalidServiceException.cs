// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using Booksy.Core.Domain.Errors;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public sealed class InvalidServiceException : ServiceCatalogDomainException
    {
        public InvalidServiceException(string message) : base(message) { }

        public override ErrorCode ErrorCode => ErrorCode.INVALID_SERVICE;
    }
}