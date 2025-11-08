// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation is not allowed
    /// </summary>
    [Serializable]
    public class OperationNotAllowedException : DomainException
    {
        public string Operation { get; }
        public string Reason { get; }
        public override string ErrorCode => "OPERATION_NOT_ALLOWED";

        public OperationNotAllowedException(string operation, string reason)
            : base($"Operation '{operation}' is not allowed: {reason}")
        {
            Operation = operation;
            Reason = reason;
            ExtensionData = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["reason"] = reason
            };
        }

        protected OperationNotAllowedException(SerializationInfo info, StreamingContext context)
         
        {
            Operation = info.GetString(nameof(Operation)) ?? "Unknown";
            Reason = info.GetString(nameof(Reason)) ?? "Unknown";
        }
    }
}
