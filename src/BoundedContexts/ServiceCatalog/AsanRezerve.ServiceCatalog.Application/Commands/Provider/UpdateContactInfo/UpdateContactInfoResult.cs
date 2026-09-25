// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/UpdateContactInfo/UpdateContactInfoResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateContactInfo
{
    public sealed record UpdateContactInfoResult(
        Guid ProviderId,
        string Email,
        string PrimaryPhone);
}