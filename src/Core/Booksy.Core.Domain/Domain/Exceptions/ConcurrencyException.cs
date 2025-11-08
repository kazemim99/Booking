// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when there's a concurrency conflict
    /// </summary>
    [Serializable]
    public class ConcurrencyException : DomainException
    {
        public string AggregateId { get; }
        public long ExpectedVersion { get; }
        public long ActualVersion { get; }
        public override ErrorCode ErrorCode => ErrorCode.CONCURRENCY_ERROR;

        public ConcurrencyException(string aggregateId, long expectedVersion, long actualVersion)
            : base($"Concurrency conflict for aggregate '{aggregateId}'. Expected version: {expectedVersion}, Actual version: {actualVersion}")
        {
            AggregateId = aggregateId;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
            ExtensionData = new Dictionary<string, object>
            {
                ["aggregateId"] = aggregateId,
                ["expectedVersion"] = expectedVersion,
                ["actualVersion"] = actualVersion
            };
        }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context)
         
        {
            AggregateId = info.GetString(nameof(AggregateId)) ?? "Unknown";
            ExpectedVersion = info.GetInt64(nameof(ExpectedVersion));
            ActualVersion = info.GetInt64(nameof(ActualVersion));
        }
    }
}
