// ========================================
// Booksy.Core.Domain/Exceptions/DomainException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Rules;
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Exceptions
{
    /// <summary>
    /// Base exception for all domain-related exceptions
    /// Follows RFC 7807 problem details specification
    /// </summary>
    [Serializable]
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// Gets the error code that uniquely identifies the type of error
        /// </summary>
        public abstract ErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets the error message (same as Message property)
        /// </summary>
        public string ErrorMessage => Message;

        /// <summary>
        /// Gets additional details for the error (extension data)
        /// </summary>
        public virtual Dictionary<string, object> Details
        {
            get => ExtensionData ?? new Dictionary<string, object>();
            protected set => ExtensionData = value;
        }

        /// <summary>
        /// Gets the category of the error for grouping purposes
        /// </summary>
        public virtual string ErrorCategory => "Domain";

        /// <summary>
        /// Gets additional extension data for the error
        /// </summary>
        public virtual Dictionary<string, object>? ExtensionData { get; protected set; }

        /// <summary>
        /// Gets the HTTP status code that should be returned for this exception
        /// </summary>
        public virtual int HttpStatusCode => 400;

        protected DomainException(string message) : base(message)
        {
            ExtensionData = new Dictionary<string, object>();
        }

        protected DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
            ExtensionData = new Dictionary<string, object>();
        }

        protected DomainException() : base()
        {
            ExtensionData = new Dictionary<string, object>();
        }

        protected DomainException(SerializationInfo info, StreamingContext context)
        {
            ExtensionData = new Dictionary<string, object>();
        }
    }
}




