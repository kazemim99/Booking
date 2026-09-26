// AsanRezerve.Core.Domain/Infrastructure/Configuration/ApiResponseOptions.cs
namespace AsanRezerve.Core.Domain.Infrastructure.Configuration
{
    public class ApiResponseOptions
    {
        public string ApiPathPrefix { get; set; } = "/api";
        public bool IncludeStackTrace { get; set; } = false;
        public bool WriteIndented { get; set; } = false;
        public string ApiVersion { get; set; } = "1.0";
        public bool IncludeMetadata { get; set; } = true;

        /// <summary>
        /// Paths whose responses are streamed as they are (not buffered, not wrapped) — e.g. the NDJSON log export,
        /// which may run to tens of megabytes.
        /// </summary>
        public string[] ExcludedPathPrefixes { get; set; } = ["/api/v1/admin/observability/logs/export"];
    }
}