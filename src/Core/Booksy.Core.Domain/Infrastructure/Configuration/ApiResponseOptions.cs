namespace Booksy.Core.Domain.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration options for API response middleware
    /// </summary>
    public class ApiResponseOptions 
    {
        public string ApiPathPrefix { get; set; } = "/api";
        public bool IncludeStackTrace { get; set; } = false;
        public bool WriteIndented { get; set; } = false;
        public string ApiVersion { get; set; } = "1.0";
        public bool IncludeMetadata { get; set; } = true;
        public int MaxValidationErrors { get; set; } = 10;
    }
}
