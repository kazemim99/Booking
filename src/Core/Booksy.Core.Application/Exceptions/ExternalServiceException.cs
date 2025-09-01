// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs
namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when an external service fails
    /// </summary>
    [Serializable]
    public class ExternalServiceException : ApplicationException
    {
        public string ServiceName { get; }
        public int? StatusCode { get; }
        public override string ErrorCode => "EXTERNAL_SERVICE_ERROR";

        public ExternalServiceException(string serviceName, string message)
            : base($"External service '{serviceName}' error: {message}")
        {
            ServiceName = serviceName;
        }

        public ExternalServiceException(string serviceName, string message, int statusCode)
            : base($"External service '{serviceName}' returned {statusCode}: {message}")
        {
            ServiceName = serviceName;
            StatusCode = statusCode;
        }

    }
}