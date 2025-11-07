using Reqnroll;
using System.Text.RegularExpressions;

namespace Booksy.ServiceCatalog.IntegrationTests.Support;

/// <summary>
/// Helper class for managing and accessing data stored in ScenarioContext
/// </summary>
public class ScenarioContextHelper
{
    private readonly ScenarioContext _scenarioContext;

    public ScenarioContextHelper(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Replace placeholders like [Provider:Current:Id] with actual values from context
    /// </summary>
    public string ReplaceContextPlaceholders(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Pattern: [EntityType:Identifier:Property]
        var regex = new Regex(@"\[(\w+):(\w+):(\w+)\]");

        return regex.Replace(value, match =>
        {
            var entityType = match.Groups[1].Value;
            var identifier = match.Groups[2].Value;
            var property = match.Groups[3].Value;

            var contextKey = $"{entityType}:{identifier}";

            if (_scenarioContext.ContainsKey(contextKey))
            {
                var entity = _scenarioContext[contextKey];
                var propertyValue = GetPropertyValue(entity, property);
                return propertyValue?.ToString() ?? value;
            }

            return value;
        });
    }

    /// <summary>
    /// Get property value from an object using reflection
    /// </summary>
    private object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj == null)
            return null;

        var property = obj.GetType().GetProperty(propertyName);
        if (property == null)
        {
            // Try nested properties (e.g., "Id.Value")
            if (propertyName.Contains('.'))
            {
                var parts = propertyName.Split('.');
                var current = obj;

                foreach (var part in parts)
                {
                    current = GetPropertyValue(current, part);
                    if (current == null)
                        return null;
                }

                return current;
            }

            return null;
        }

        return property.GetValue(obj);
    }

    /// <summary>
    /// Parse relative time strings like "2 days from now at 10:00"
    /// </summary>
    public DateTime ParseRelativeTime(string timeString)
    {
        if (DateTime.TryParse(timeString, out var absoluteTime))
            return absoluteTime;

        // Parse "X days from now at HH:mm"
        var futureMatch = Regex.Match(timeString, @"(\d+) days? from now at (\d+):(\d+)");
        if (futureMatch.Success)
        {
            var days = int.Parse(futureMatch.Groups[1].Value);
            var hours = int.Parse(futureMatch.Groups[2].Value);
            var minutes = int.Parse(futureMatch.Groups[3].Value);

            return DateTime.UtcNow.AddDays(days).Date.AddHours(hours).AddMinutes(minutes);
        }

        // Parse "X days ago at HH:mm"
        var pastMatch = Regex.Match(timeString, @"(\d+) days? ago at (\d+):(\d+)");
        if (pastMatch.Success)
        {
            var days = int.Parse(pastMatch.Groups[1].Value);
            var hours = int.Parse(pastMatch.Groups[2].Value);
            var minutes = int.Parse(pastMatch.Groups[3].Value);

            return DateTime.UtcNow.AddDays(-days).Date.AddHours(hours).AddMinutes(minutes);
        }

        // Parse "tomorrow at HH:mm"
        var tomorrowMatch = Regex.Match(timeString, @"tomorrow at (\d+):(\d+)");
        if (tomorrowMatch.Success)
        {
            var hours = int.Parse(tomorrowMatch.Groups[1].Value);
            var minutes = int.Parse(tomorrowMatch.Groups[2].Value);

            return DateTime.UtcNow.AddDays(1).Date.AddHours(hours).AddMinutes(minutes);
        }

        throw new ArgumentException($"Unable to parse time string: {timeString}");
    }

    /// <summary>
    /// Convert string value to appropriate type
    /// </summary>
    public object ConvertValueToType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Replace context placeholders first
        value = ReplaceContextPlaceholders(value);

        // Try GUID
        if (Guid.TryParse(value, out var guidValue))
            return guidValue;

        // Try decimal
        if (decimal.TryParse(value, out var decimalValue))
            return decimalValue;

        // Try int
        if (int.TryParse(value, out var intValue))
            return intValue;

        // Try bool
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        // Try DateTime (including relative times)
        try
        {
            return ParseRelativeTime(value);
        }
        catch
        {
            // Not a time string, continue
        }

        // Return as string
        return value;
    }

    /// <summary>
    /// Build a dictionary from a Reqnroll table
    /// </summary>
    public Dictionary<string, object> BuildDictionaryFromTable(Table table)
    {
        var result = new Dictionary<string, object>();

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var value = row["Value"];

            result[field] = ConvertValueToType(value);
        }

        return result;
    }
}
