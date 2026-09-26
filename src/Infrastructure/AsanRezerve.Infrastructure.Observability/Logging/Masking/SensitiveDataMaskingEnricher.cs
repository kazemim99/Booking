using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.Logging.Masking;

/// <summary>
/// Masks secrets and personal data in every log event before any sink sees it (console, file, Seq, the log store).
/// <list type="bullet">
/// <item>Secrets — passwords, OTP and verification codes, tokens, secrets, API keys, authorization values, card
/// data — become <c>***</c>, whatever their value.</item>
/// <item>Phone numbers keep their first four and last two characters (<c>0912*****67</c>); e-mail addresses keep
/// their first character and domain (<c>a***@example.com</c>). Matched by property name, and by value for any
/// string that is an Iranian mobile number or an e-mail address, whatever the property is called.</item>
/// </list>
/// Destructured objects (<c>{@Request}</c>), sequences and dictionaries are walked to any depth. Values that already
/// contain <c>*</c> are left alone. A secret interpolated into a message <em>string</em> (not a property) cannot be
/// seen here; call sites must not do that.
/// </summary>
public sealed partial class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    public const string Redacted = "***";

    private static readonly HashSet<string> SecretNames = new(StringComparer.Ordinal)
    {
        "password", "code", "otp", "otpcode", "verificationcode", "twofactorcode", "sandboxcode",
        "token", "accesstoken", "refreshtoken", "idtoken", "secret", "clientsecret", "secretkey",
        "apikey", "authorization", "cardnumber", "pan", "cvv", "cvv2", "pin", "pincode",
    };

    private static readonly string[] SecretSuffixes = ["password", "passwordhash", "token", "secret", "apikey", "otp", "otpcode"];

    private static readonly HashSet<string> PhoneNames = new(StringComparer.Ordinal)
    {
        "phone", "phonenumber", "mobile", "mobilenumber", "primaryphone", "secondaryphone", "cellphone", "recipient", "to", "receptor",
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        List<LogEventProperty>? changed = null;

        foreach (var (name, value) in logEvent.Properties)
        {
            var masked = Mask(name, value);
            if (!ReferenceEquals(masked, value))
                (changed ??= []).Add(new LogEventProperty(name, masked));
        }

        if (changed is null) return;
        foreach (var property in changed)
            logEvent.AddOrUpdateProperty(property);
    }

    /// <summary>The masked form of <paramref name="value"/>; the same instance when nothing needed masking.</summary>
    internal static LogEventPropertyValue Mask(string? name, LogEventPropertyValue value)
    {
        var kind = Classify(name);
        return value switch
        {
            ScalarValue scalar => MaskScalar(kind, scalar),
            StructureValue structure => MaskStructure(structure),
            SequenceValue sequence => MaskSequence(name, sequence),
            DictionaryValue dictionary => MaskDictionary(dictionary),
            _ => value,
        };
    }

    private enum Kind { None, Secret, Phone, Email }

    private static Kind Classify(string? name)
    {
        if (string.IsNullOrEmpty(name)) return Kind.None;

        var key = Normalize(name);
        if (IsSecret(key) || (key.Length > 1 && key[^1] == 's' && IsSecret(key[..^1]))) return Kind.Secret; // Tokens, Passwords
        if (PhoneNames.Contains(key) || key.Contains("phone", StringComparison.Ordinal) || key.Contains("mobile", StringComparison.Ordinal)) return Kind.Phone;
        if (key.Contains("email", StringComparison.Ordinal)) return Kind.Email;
        return Kind.None;
    }

    private static bool IsSecret(string key) => SecretNames.Contains(key) || SecretSuffixes.Any(key.EndsWith);

    private static string Normalize(string name)
    {
        Span<char> buffer = stackalloc char[Math.Min(name.Length, 128)];
        var length = 0;
        foreach (var c in name)
        {
            if (c is '_' or '-' or ' ' || length == buffer.Length) continue;
            buffer[length++] = char.ToLowerInvariant(c);
        }
        return new string(buffer[..length]);
    }

    private static LogEventPropertyValue MaskScalar(Kind kind, ScalarValue scalar)
    {
        if (scalar.Value is null) return scalar;

        if (kind == Kind.Secret) return new ScalarValue(Redacted);

        if (scalar.Value is not string text || text.Contains('*')) return scalar;

        // By value first: a `{To}` or `{Recipient}` may hold an e-mail address as well as a phone number.
        if (IsEmail(text)) return new ScalarValue(MaskEmail(text));
        if (IsMobileNumber(text) || kind == Kind.Phone) return new ScalarValue(MaskPhone(text));
        if (kind == Kind.Email) return new ScalarValue(MaskEmail(text));
        return scalar;
    }

    private static LogEventPropertyValue MaskStructure(StructureValue structure)
    {
        LogEventProperty[]? masked = null;
        var properties = structure.Properties;
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var value = Mask(property.Name, property.Value);
            if (ReferenceEquals(value, property.Value)) continue;

            masked ??= properties.ToArray();
            masked[i] = new LogEventProperty(property.Name, value);
        }
        return masked is null ? structure : new StructureValue(masked, structure.TypeTag);
    }

    private static LogEventPropertyValue MaskSequence(string? name, SequenceValue sequence)
    {
        // Elements inherit the sequence's name: a `Tokens` list is a list of tokens.
        LogEventPropertyValue[]? masked = null;
        var elements = sequence.Elements;
        for (var i = 0; i < elements.Count; i++)
        {
            var value = Mask(name, elements[i]);
            if (ReferenceEquals(value, elements[i])) continue;

            masked ??= elements.ToArray();
            masked[i] = value;
        }
        return masked is null ? sequence : new SequenceValue(masked);
    }

    private static LogEventPropertyValue MaskDictionary(DictionaryValue dictionary)
    {
        var changed = false;
        var masked = dictionary.Elements.Select(kv =>
        {
            var value = Mask(kv.Key.Value?.ToString(), kv.Value);
            changed |= !ReferenceEquals(value, kv.Value);
            return new KeyValuePair<ScalarValue, LogEventPropertyValue>(kv.Key, value);
        }).ToList();
        return changed ? new DictionaryValue(masked) : dictionary;
    }

    internal static bool IsMobileNumber(string text) =>
        text.Length is >= 10 and <= 14 && MobileNumber().IsMatch(text);

    internal static bool IsEmail(string text) =>
        text.Length <= 254 && text.Contains('@') && Email().IsMatch(text);

    internal static string MaskPhone(string text)
    {
        if (text.Length < 7) return Redacted;
        return string.Concat(text.AsSpan(0, 4), new string('*', text.Length - 6), text.AsSpan(text.Length - 2));
    }

    internal static string MaskEmail(string text)
    {
        var at = text.IndexOf('@');
        return at <= 0 ? Redacted : string.Concat(text.AsSpan(0, 1), "***", text.AsSpan(at));
    }

    /// <summary>
    /// Masks phone numbers and e-mail addresses inside free text — exception messages and stack traces, which are
    /// stored as text rather than properties. Best effort; see the class remarks.
    /// </summary>
    public static string? ScrubText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var scrubbed = MobileNumberInText().Replace(text, m => MaskPhone(m.Value));
        return EmailInText().Replace(scrubbed, m => MaskEmail(m.Value));
    }

    [GeneratedRegex(@"(?<![\w+])(\+98|0098|0)?9\d{9}(?!\w)", RegexOptions.CultureInvariant)]
    private static partial Regex MobileNumberInText();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailInText();

    [GeneratedRegex(@"^(\+98|0098|98|0)?9\d{9}$", RegexOptions.CultureInvariant)]
    private static partial Regex MobileNumber();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Email();
}
