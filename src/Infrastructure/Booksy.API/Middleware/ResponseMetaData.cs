// Booksy.Core.Domain/Infrastructure/Middleware/ApiResponseMiddleware.cs

namespace Booksy.Core.Domain.Infrastructure.Middleware
{
    public class ResponseMetaData
    {
        public string requestId { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public long duration { get; set; }
        public string path { get; set; }
        public string method { get; set; }
        public string version { get; set; }
    }
}