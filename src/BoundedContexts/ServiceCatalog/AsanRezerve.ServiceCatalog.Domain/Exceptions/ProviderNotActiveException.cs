// ========================================
// AsanRezerve.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Exceptions
{
    public sealed class ProviderNotActiveException : ServiceCatalogDomainException
    {
        public ProviderNotActiveException(ProviderId providerId)
            : base($"Provider {providerId} is not active") { }

        public override string ErrorCode => "PROVIDER_NOT_ACTIVE";
    }
}