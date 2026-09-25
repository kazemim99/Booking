// ========================================
// AsanRezerve.ServiceCatalog.Domain/Exceptions/ServiceCatalogDomainException.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Domain.Exceptions
{
    public sealed class InvalidServiceException : ServiceCatalogDomainException
    {
        public InvalidServiceException(string message) : base(message) { }

        public override string ErrorCode => "INVALID_SERVICE";
    }
}