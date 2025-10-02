// Booksy.Core.Domain/Infrastructure/Configuration/ApiResponseOptions.cs
namespace Booksy.Core.Domain.Infrastructure.Configuration
{
    public class ApiResponseOptions
    {
        public string ApiPathPrefix { get; set; } = "/api";
        public bool IncludeStackTrace { get; set; } = false;
        public bool WriteIndented { get; set; } = false;
        public string ApiVersion { get; set; } = "1.0";
        public bool IncludeMetadata { get; set; } = true;
    }
}