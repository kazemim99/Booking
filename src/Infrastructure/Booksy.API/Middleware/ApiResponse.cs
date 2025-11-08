
using Booksy.Core.Domain.Exceptions;

namespace Booksy.Core.Domain.Infrastructure.Middleware
{
    public class ApiResponse<TResponse> 
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public ApiError Error { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
        public TResponse Data { get; set; }
        public ResponseMetaData Metadata { get; internal set; }
    }
    public class ApiResponse
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public ApiError Error { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
        public object Data { get; set; }
        public ResponseMetaData Metadata { get; internal set; }
    }
    public class ApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }

    }
}