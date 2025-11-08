// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a resource limit is exceeded
    /// </summary>
    [Serializable]
    public class ResourceLimitExceededException : DomainException
    {
        public string ResourceType { get; }
        public int CurrentCount { get; }
        public int MaximumAllowed { get; }
        public override ErrorCode ErrorCode => ErrorCode.RESOURCE_LIMIT_EXCEEDED;

        public ResourceLimitExceededException(string resourceType, int currentCount, int maximumAllowed)
            : base($"{resourceType} limit exceeded. Current: {currentCount}, Maximum: {maximumAllowed}")
        {
            ResourceType = resourceType;
            CurrentCount = currentCount;
            MaximumAllowed = maximumAllowed;
            ExtensionData = new Dictionary<string, object>
            {
                ["resourceType"] = resourceType,
                ["currentCount"] = currentCount,
                ["maximumAllowed"] = maximumAllowed
            };
        }

        protected ResourceLimitExceededException(SerializationInfo info, StreamingContext context)
         
        {
            ResourceType = info.GetString(nameof(ResourceType)) ?? "Unknown";
            CurrentCount = info.GetInt32(nameof(CurrentCount));
            MaximumAllowed = info.GetInt32(nameof(MaximumAllowed));
        }
    }
}
