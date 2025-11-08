// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using Booksy.Core.Domain.Errors;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public sealed class InvalidProviderException : ServiceCatalogDomainException
    {
        public InvalidProviderException(string message) : base(message) { }

        public override ErrorCode ErrorCode => ErrorCode.INVALID_PROVIDER;
    }
}