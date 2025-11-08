// ========================================
// Booksy.API/Models/ProblemDetailsResponse.cs
// ========================================

using Booksy.Core.Domain.Errors;
using System.Text.Json.Serialization;

namespace Booksy.API.Models
{
    /// <summary>
    /// RFC 7807 Problem Details response model
    /// </summary>
    public class ProblemDetailsResponse
    {
        /// <summary>
        /// A URI reference that identifies the problem type
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "about:blank";

        /// <summary>
        /// A short, human-readable summary of the problem type
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The HTTP status code
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem
        /// </summary>
        [JsonPropertyName("detail")]
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// A URI reference that identifies the specific occurrence of the problem
        /// </summary>
        [JsonPropertyName("instance")]
        public string? Instance { get; set; }

        /// <summary>
        /// The error code enum as string
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Additional extension members
        /// </summary>
        [JsonPropertyName("extensions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Extensions { get; set; }

        /// <summary>
        /// Creates a Problem Details response from an error code and details
        /// </summary>
        public static ProblemDetailsResponse Create(
            ErrorCode errorCode,
            int statusCode,
            string detail,
            string? instance = null,
            Dictionary<string, object>? extensions = null)
        {
            return new ProblemDetailsResponse
            {
                Type = $"https://docs.booksy.com/errors/{errorCode}",
                Title = GetTitleFromStatusCode(statusCode),
                Status = statusCode,
                Detail = detail,
                Instance = instance,
                ErrorCode = errorCode.ToString(),
                Extensions = extensions
            };
        }

        private static string GetTitleFromStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                422 => "Unprocessable Entity",
                500 => "Internal Server Error",
                503 => "Service Unavailable",
                _ => "Error"
            };
        }
    }
}
