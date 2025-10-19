namespace Booksy.ServiceCatalog.Application.Exceptions;

/// <summary>
/// Exception thrown when validation fails with detailed field-level errors
/// </summary>
public sealed class ValidationException : Exception
{
    public Dictionary<string, List<string>> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, List<string>>();
    }

    public ValidationException(Dictionary<string, List<string>> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public ValidationException(string field, string errorMessage)
        : base($"Validation failed for {field}")
    {
        Errors = new Dictionary<string, List<string>>
        {
            { field, new List<string> { errorMessage } }
        };
    }

    public void AddError(string field, string errorMessage)
    {
        if (!Errors.ContainsKey(field))
        {
            Errors[field] = new List<string>();
        }
        Errors[field].Add(errorMessage);
    }
}
