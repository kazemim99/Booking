// AsanRezerve.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using System.Runtime.Serialization;

// AsanRezerve.Application.Common/Exceptions/ApplicationExceptions.cs
namespace AsanRezerve.Core.Application.Exceptions
{
    /// <summary>
    /// Base exception for application layer exceptions
    /// </summary>
    [Serializable]
    public abstract class ApplicationException : Exception
    {
        public abstract string ErrorCode { get; }
        public virtual string? ErrorCategory => "Application";

        protected ApplicationException(string message) : base(message) { }
        protected ApplicationException(string message, Exception innerException)
            : base(message, innerException) { }
        protected ApplicationException(SerializationInfo info, StreamingContext context)
          { }
    }
}