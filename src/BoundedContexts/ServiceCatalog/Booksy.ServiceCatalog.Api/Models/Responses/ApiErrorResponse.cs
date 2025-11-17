namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Unified API error response for consistent client-side error handling
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of errors that occurred
    /// </summary>
    public List<ErrorDetail> Errors { get; set; } = new();

    /// <summary>
    /// Optional trace ID for debugging (5xx errors)
    /// </summary>
    public string? TraceId { get; set; }

    public ApiErrorResponse()
    {
        Success = false;
    }

    public ApiErrorResponse(string code, string message, string? field = null)
    {
        Success = false;
        Errors = new List<ErrorDetail>
        {
            new ErrorDetail
            {
                Code = code,
                Message = message,
                Field = field
            }
        };
    }

    public ApiErrorResponse(List<ErrorDetail> errors)
    {
        Success = false;
        Errors = errors;
    }
}

/// <summary>
/// Detailed error information
/// </summary>
public sealed class ErrorDetail
{
    /// <summary>
    /// Error code for programmatic handling (e.g., "ERR_VALIDATION", "ERR_DUPLICATE")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional field name for validation errors (e.g., "businessName", "services[0].price")
    /// </summary>
    public string? Field { get; set; }
}
