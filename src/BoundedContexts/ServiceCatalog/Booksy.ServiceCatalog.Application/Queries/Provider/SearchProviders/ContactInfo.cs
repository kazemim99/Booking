namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed record ContactInfo(
        string? Email,
        string? Phone,
        string? SecondaryPhone,
        string? Website);
}
