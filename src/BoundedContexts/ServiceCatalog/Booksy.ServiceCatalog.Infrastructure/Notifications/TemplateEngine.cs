// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/TemplateEngine.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using System.Text.RegularExpressions;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications
{
    /// <summary>
    /// Template rendering engine with variable substitution
    /// Supports {{variableName}} and {{variableName:format}} syntax
    /// </summary>
    public class TemplateEngine : ITemplateEngine
    {
        // Regex to match {{variableName}} or {{variableName:format}}
        private static readonly Regex VariablePattern = new Regex(
            @"\{\{([a-zA-Z0-9_]+)(?::([^\}]+))?\}\}",
            RegexOptions.Compiled);

        public string Render(string template, Dictionary<string, object> variables)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            if (variables == null || variables.Count == 0)
                return template;

            return VariablePattern.Replace(template, match =>
            {
                var variableName = match.Groups[1].Value;
                var format = match.Groups[2].Success ? match.Groups[2].Value : null;

                // Case-insensitive variable lookup
                var key = variables.Keys.FirstOrDefault(k =>
                    k.Equals(variableName, StringComparison.OrdinalIgnoreCase));

                if (key == null)
                {
                    // Variable not found, return placeholder as-is (or empty string)
                    return string.Empty;
                }

                var value = variables[key];

                // Handle null values
                if (value == null)
                    return string.Empty;

                // Apply formatting if specified
                if (!string.IsNullOrEmpty(format))
                {
                    return FormatValue(value, format);
                }

                return value.ToString() ?? string.Empty;
            });
        }

        public List<string> ValidateVariables(List<string> requiredVariables, Dictionary<string, object> providedVariables)
        {
            if (requiredVariables == null || requiredVariables.Count == 0)
                return new List<string>();

            if (providedVariables == null)
                return requiredVariables.ToList();

            var missingVariables = new List<string>();
            var providedKeys = providedVariables.Keys.Select(k => k.ToLowerInvariant()).ToHashSet();

            foreach (var required in requiredVariables)
            {
                if (!providedKeys.Contains(required.ToLowerInvariant()))
                {
                    missingVariables.Add(required);
                }
            }

            return missingVariables;
        }

        public List<string> ExtractVariables(string template)
        {
            if (string.IsNullOrEmpty(template))
                return new List<string>();

            var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var matches = VariablePattern.Matches(template);
            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value;
                variables.Add(variableName);
            }

            return variables.ToList();
        }

        /// <summary>
        /// Formats a value according to the specified format string
        /// Supports: date, datetime, time, currency, number, uppercase, lowercase, etc.
        /// </summary>
        private string FormatValue(object value, string format)
        {
            try
            {
                switch (format.ToLowerInvariant())
                {
                    case "uppercase":
                    case "upper":
                        return value.ToString()?.ToUpperInvariant() ?? string.Empty;

                    case "lowercase":
                    case "lower":
                        return value.ToString()?.ToLowerInvariant() ?? string.Empty;

                    case "date":
                        if (value is DateTime dt)
                            return dt.ToString("yyyy-MM-dd");
                        if (value is DateTimeOffset dto)
                            return dto.ToString("yyyy-MM-dd");
                        break;

                    case "datetime":
                        if (value is DateTime dtFull)
                            return dtFull.ToString("yyyy-MM-dd HH:mm");
                        if (value is DateTimeOffset dtoFull)
                            return dtoFull.ToString("yyyy-MM-dd HH:mm");
                        break;

                    case "time":
                        if (value is DateTime dtTime)
                            return dtTime.ToString("HH:mm");
                        if (value is DateTimeOffset dtoTime)
                            return dtoTime.ToString("HH:mm");
                        if (value is TimeSpan ts)
                            return ts.ToString(@"hh\:mm");
                        break;

                    case "longdate":
                        if (value is DateTime dtLong)
                            return dtLong.ToString("dddd, MMMM dd, yyyy");
                        if (value is DateTimeOffset dtoLong)
                            return dtoLong.ToString("dddd, MMMM dd, yyyy");
                        break;

                    case "currency":
                    case "money":
                        if (value is decimal decVal)
                            return decVal.ToString("C");
                        if (double.TryParse(value.ToString(), out var dblVal))
                            return dblVal.ToString("C");
                        break;

                    case "number":
                        if (value is int || value is long || value is decimal || value is double)
                            return string.Format("{0:N0}", value);
                        break;

                    default:
                        // Try to use format as a standard .NET format string
                        if (value is IFormattable formattable)
                            return formattable.ToString(format, null);
                        break;
                }
            }
            catch
            {
                // If formatting fails, return the value as-is
            }

            return value.ToString() ?? string.Empty;
        }
    }
}
