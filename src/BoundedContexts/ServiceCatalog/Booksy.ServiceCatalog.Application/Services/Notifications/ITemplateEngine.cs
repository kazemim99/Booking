// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/ITemplateEngine.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Service for rendering notification templates with variable substitution
    /// </summary>
    public interface ITemplateEngine
    {
        /// <summary>
        /// Renders a template by replacing placeholders with actual values
        /// </summary>
        /// <param name="template">Template string with placeholders like {{variableName}}</param>
        /// <param name="variables">Dictionary of variable names to values</param>
        /// <returns>Rendered template with substituted values</returns>
        string Render(string template, Dictionary<string, object> variables);

        /// <summary>
        /// Validates that all required variables are present in the data
        /// </summary>
        /// <param name="requiredVariables">List of required variable names</param>
        /// <param name="providedVariables">Dictionary of provided variables</param>
        /// <returns>List of missing variable names (empty if all present)</returns>
        List<string> ValidateVariables(List<string> requiredVariables, Dictionary<string, object> providedVariables);

        /// <summary>
        /// Extracts all placeholder variable names from a template
        /// </summary>
        /// <param name="template">Template string</param>
        /// <returns>List of unique variable names found in template</returns>
        List<string> ExtractVariables(string template);
    }
}
