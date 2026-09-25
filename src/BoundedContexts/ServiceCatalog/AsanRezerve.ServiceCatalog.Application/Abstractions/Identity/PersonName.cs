namespace AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;

/// <summary>What counts as somebody's real name.</summary>
/// <remarks>
/// A person created by OTP without a name is stored as «مشتری 9123456789» / «ارائه‌دهنده 9123…»
/// (PersonProvisioningService): the word for their side of the marketplace, then their phone's national number.
/// That is not a name, and a phone number is never part of one — "the number must never be written anywhere"
/// (production QA 2026-09-23). Every API that names a person goes through this class.
/// </remarks>
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

    /// <summary>A run of digits this long is a phone number, however it is spaced; shorter ones can be part of a name.</summary>
    private const int PhoneDigits = 7;

    /// <summary>The person's real name from their two parts, or null when they have none.</summary>
    public static string? RealOrNull(string? firstName, string? lastName)
    {
        var (first, last) = RealParts(firstName, lastName);
        var full = $"{first} {last}".Trim();
        return full.Length == 0 ? null : full;
    }

    /// <summary>
    /// Each part with the placeholder and any phone number taken out — blank when nothing real is left. For APIs
    /// that send first and last name separately, so a client joining them can never rebuild the placeholder.
    /// </summary>
    public static (string First, string Last) RealParts(string? firstName, string? lastName)
    {
        var first = Sanitize(firstName) ?? string.Empty;

        // The placeholder's surname is the phone's national number: a surname made of digits is not one.
        var last = lastName?.Trim() ?? string.Empty;
        last = IsDigitGroup(last) ? string.Empty : Sanitize(last) ?? string.Empty;

        return (first, last);
    }

    /// <summary>
    /// A name held as one string (a salon-given display name, a stored full name) with any phone number taken
    /// out; null when what is left is nothing or the bare placeholder word.
    /// </summary>
    public static string? Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Split on real whitespace only: the ZWNJ inside «ارائه‌دهنده» is part of the word.
        var tokens = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var kept = new List<string>(tokens.Length);
        var run = new List<string>();

        void EndRun()
        {
            // Consecutive digit groups ("0912 313 5143") are one number; drop it when it is a phone.
            if (run.Sum(DigitCount) < PhoneDigits)
                kept.AddRange(run);
            run.Clear();
        }

        foreach (var token in tokens)
        {
            if (IsDigitGroup(token))
            {
                run.Add(token);
                continue;
            }

            EndRun();
            kept.Add(token);
        }

        EndRun();

        var result = string.Join(' ', kept);
        return result.Length == 0 || PlaceholderFirstNames.Contains(result) ? null : result;
    }

    /// <summary>
    /// What a customer sees for a salon member: the person's real name; else the name the salon gave them; else
    /// the salon's own name. Never a placeholder, never a phone number.
    /// </summary>
    public static string ForMember(PersonInfo? person, string? displayName, string salonName) =>
        (person is null ? null : RealOrNull(person.FirstName, person.LastName))
        ?? Sanitize(displayName)
        ?? salonName;

    /// <summary>Digits, optionally with the separators a phone number is written with; at least one digit.</summary>
    private static bool IsDigitGroup(string token) =>
        token.Length > 0
        && token.Any(char.IsDigit)
        && token.All(c => char.IsDigit(c) || c is '+' or '-' or '(' or ')' or '.' or '/');

    private static int DigitCount(string token) => token.Count(char.IsDigit);
}
