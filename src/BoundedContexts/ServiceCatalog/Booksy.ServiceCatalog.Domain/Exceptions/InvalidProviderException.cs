// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    public sealed class InvalidProviderException : ServiceCatalogDomainException
    {
        public InvalidProviderException(string message) : base(message) { }

        public override string ErrorCode => "INVALID_PROVIDER";
    }  
}