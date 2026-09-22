namespace Booksy.ServiceCatalog.Application.Abstractions.Identity;

/// <summary>What counts as somebody's real name.</summary>
public static class PersonName
{
    /// <summary>
    /// The words a person gets when created by OTP without a name — «مشتری 9123456789», «ارائه‌دهنده 9123…».
    /// Printing those as a name ("مشتری 9123456789 عزیز") is worse than the neutral fallback.
    /// </summary>
    private static readonly HashSet<string> PlaceholderFirstNames = new(StringComparer.Ordinal)
    {
        "مشتری", "ارائه‌دهنده", "ارائه دهنده",
    };

    public static string? RealOrNull(string? firstName, string? lastName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;

        if (PlaceholderFirstNames.Contains(first))
            first = string.Empty;
        // The placeholder's last name is the phone's national number.
        if (last.Length > 0 && last.All(char.IsDigit))
            last = string.Empty;

        var full = $"{first} {last}".Trim();
        return full.Length == 0 ? null : full;
    }
}
