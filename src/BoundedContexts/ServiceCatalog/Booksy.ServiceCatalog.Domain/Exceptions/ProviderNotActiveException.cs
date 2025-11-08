// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using Booksy.Core.Domain.Errors;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public sealed class ProviderNotActiveException : ServiceCatalogDomainException
    {
        public ProviderNotActiveException(ProviderId providerId)
            : base($"Provider {providerId} is not active") { }

        public override ErrorCode ErrorCode => ErrorCode.PROVIDER_NOT_ACTIVE;
    }
}