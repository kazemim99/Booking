namespace AsanRezerve.ServiceCatalog.Application.Caching;

/// <summary>
/// Tags of ServiceCatalog's cached reads, and which of them a change evicts (add-observability-and-caching, D8).
/// </summary>
public static class ReadModelCacheTags
{
    /// <summary>Salon lists (search). Evicted by any provider change.</summary>
    public const string ProviderDirectory = "provider-directory";

    /// <summary>Categories with their provider counts.</summary>
    public const string Categories = "categories";

    /// <summary>Province/city reference data.</summary>
    public const string Locations = "locations";

    /// <summary>Everything cached about one salon (its page).</summary>
    public static string Provider(Guid providerId) => $"provider:{providerId}";

    /// <summary>What a change to a salon — its profile, hours, gallery, services, staff, status — evicts.</summary>
    public static string[] ForProviderChange(Guid providerId) =>
        [Provider(providerId), ProviderDirectory, Categories];
}
