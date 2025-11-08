// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a domain invariant is violated
    /// </summary>
    [Serializable]
    public class InvariantViolationException : DomainException
    {
        public string InvariantName { get; }
        public string? InvariantDescription { get; }
        public override ErrorCode ErrorCode => ErrorCode.INVARIANT_VIOLATION;

        public InvariantViolationException(string message, string invariantName)
            : base(message)
        {
            InvariantName = invariantName;
            ExtensionData = new Dictionary<string, object>
            {
                ["invariantName"] = invariantName
            };
        }

        public InvariantViolationException(
            string message,
            string invariantName,
            string invariantDescription)
            : base(message)
        {
            InvariantName = invariantName;
            InvariantDescription = invariantDescription;
            ExtensionData = new Dictionary<string, object>
            {
                ["invariantName"] = invariantName,
                ["invariantDescription"] = invariantDescription
            };
        }

        protected InvariantViolationException(SerializationInfo info, StreamingContext context)
         
        {
            InvariantName = info.GetString(nameof(InvariantName)) ?? "Unknown";
            InvariantDescription = info.GetString(nameof(InvariantDescription));
        }
    }
}
