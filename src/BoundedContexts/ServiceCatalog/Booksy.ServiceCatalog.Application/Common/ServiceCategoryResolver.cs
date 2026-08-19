using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;

namespace Booksy.ServiceCatalog.Application.Common;

/// <summary>
/// Single place that turns the category string arriving from a client into a
/// <see cref="ServiceCategory"/>.
///
/// <para>Registration payloads have historically carried the category in three different shapes —
/// the numeric id, the enum member name, and a frontend taxonomy id such as <c>hair_salon</c> —
/// and each command handler grew its own parser. Those parsers disagreed: the registration wizard
/// sends <c>barber</c> for a men's barbershop, which no handler's map listed, so every barbershop
/// silently fell through to the <c>BeautySalon</c> default. Another handler used a bare
/// <c>Enum.TryParse</c>, which accepts any number and would happily produce an undefined category.</para>
///
/// <para>Everything now resolves here, in one order, against the same slug table the rest of the
/// system uses.</para>
/// </summary>
public static class ServiceCategoryResolver
{
    /// <summary>
    /// Taxonomy ids the frontend has used that are not category slugs in their own right.
    /// These are narrower marketing categories folded into the category that owns them.
    /// </summary>
    private static readonly Dictionary<string, ServiceCategory> LegacyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brows_lashes"] = ServiceCategory.BeautySalon,
        ["braids_locs"] = ServiceCategory.HairSalon,
        ["aesthetic_medicine"] = ServiceCategory.MedicalClinic,
        ["dental_orthodontics"] = ServiceCategory.Dental,
        ["hair_removal"] = ServiceCategory.Spa,
        ["health_fitness"] = ServiceCategory.Gym,
        ["beauty_spa"] = ServiceCategory.BeautySalon,
        ["other"] = ServiceCategory.BeautySalon
    };

    /// <summary>
    /// Attempts to resolve a client-supplied category string.
    /// </summary>
    /// <param name="value">Numeric id, enum member name, category slug, or legacy taxonomy id.</param>
    /// <param name="category">The resolved category, or <c>default</c> when unresolved.</param>
    /// <returns>True when <paramref name="value"/> names a declared ServiceCategory.</returns>
    public static bool TryResolve(string? value, out ServiceCategory category)
    {
        category = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var input = value.Trim();

        // Numeric id ("1"). Checked against the declared members so an out-of-range number is
        // rejected rather than becoming a category that has no metadata.
        if (int.TryParse(input, out var id))
        {
            var candidate = (ServiceCategory)id;
            if (candidate.IsDefinedCategory())
            {
                category = candidate;
                return true;
            }

            return false;
        }

        // Enum member name ("HairSalon", "hairSalon" — the API's own serialised form).
        if (Enum.TryParse<ServiceCategory>(input, ignoreCase: true, out var parsed)
            && parsed.IsDefinedCategory())
        {
            category = parsed;
            return true;
        }

        // Category slug ("hair-salon", "hair_salon", "barber", ...).
        if (ServiceCategoryExtensions.TryParseSlug(input, out var fromSlug))
        {
            category = fromSlug;
            return true;
        }

        // Legacy frontend taxonomy ids.
        if (LegacyAliases.TryGetValue(input, out var alias))
        {
            category = alias;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a client-supplied category string, falling back when it cannot be resolved.
    /// Use on paths that must not fail registration over an unrecognised category; prefer
    /// <see cref="TryResolve"/> where rejecting the request is the better outcome.
    /// </summary>
    public static ServiceCategory Resolve(string? value, ServiceCategory fallback)
        => TryResolve(value, out var category) ? category : fallback;
}
