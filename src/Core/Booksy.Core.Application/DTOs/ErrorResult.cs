// ========================================
// Booksy.Core.Application/DTOs/ErrorResult.cs
// ========================================
namespace Booksy.Core.Application.DTOs
{
    /// <summary>
    /// Represents error information
    /// </summary>
    public sealed class ErrorResult
    {
        public string Code { get; }
        public string Message { get; }
        public Dictionary<string, object> Metadata { get; }

        public ErrorResult(string code, string message, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Error code cannot be empty", nameof(code));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message cannot be empty", nameof(message));

            Code = code;
            Message = message;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public ErrorResult WithMetadata(string key, object value)
        {
            var newMetadata = new Dictionary<string, object>(Metadata)
            {
                [key] = value
            };
            return new ErrorResult(Code, Message, newMetadata);
        }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }

        // Common error results
        public static ErrorResult NotFound(string entityName, object entityId)
        {
            return new ErrorResult(
                "NOT_FOUND",
                $"{entityName} with id '{entityId}' was not found",
                new Dictionary<string, object>
                {
                    ["EntityName"] = entityName,
                    ["EntityId"] = entityId
                });
        }

        public static ErrorResult ValidationFailed(string message)
        {
            return new ErrorResult("VALIDATION_FAILED", message);
        }

        public static ErrorResult Unauthorized(string message = "You are not authorized to perform this action")
        {
            return new ErrorResult("UNAUTHORIZED", message);
        }

        public static ErrorResult Forbidden(string message = "Access to this resource is forbidden")
        {
            return new ErrorResult("FORBIDDEN", message);
        }

        public static ErrorResult Conflict(string message)
        {
            return new ErrorResult("CONFLICT", message);
        }

        public static ErrorResult InternalError(string message = "An internal error occurred")
        {
            return new ErrorResult("INTERNAL_ERROR", message);
        }
    }


}
