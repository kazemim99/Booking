// ========================================
// Middleware/ApiErrorResult.cs
// ========================================
namespace Booksy.API.Middleware;

public partial class ExceptionHandlingMiddleware
{
    public class ApiErrorResult
    {
        public string Message { get; set; }
        public string? Code { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }

        public ApiErrorResult(string message, string? code = null)
        {
            Message = message;
            Code = code;
        }
    }
}


