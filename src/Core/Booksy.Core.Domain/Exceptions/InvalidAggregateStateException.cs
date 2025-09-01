// ========================================
// Booksy.Core.Domain/Exceptions/InvalidAggregateStateException.cs
// ========================================
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an aggregate is in an invalid state for the requested operation
    /// </summary>
    [Serializable]
    public sealed class InvalidAggregateStateException : DomainException
    {
        public override string ErrorCode => "INVALID_AGGREGATE_STATE";
        public string AggregateName { get; }
        public string Operation { get; }
        public string CurrentState { get; }

        public InvalidAggregateStateException(
            string aggregateName,
            string operation,
            string currentState,
            string message)
            : base(message)
        {
            AggregateName = aggregateName;
            Operation = operation;
            CurrentState = currentState;

            ExtensionData = new Dictionary<string, object>
            {
                ["aggregateName"] = AggregateName,
                ["operation"] = Operation,
                ["currentState"] = CurrentState
            };
        }

        public InvalidAggregateStateException(
            Type aggregateType,
            string operation,
            string currentState)
            : this(
                aggregateType.Name,
                operation,
                currentState,
                $"The {aggregateType.Name} aggregate is in state '{currentState}' which is invalid for operation '{operation}'")
        {
        }

        private InvalidAggregateStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            AggregateName = info.GetString(nameof(AggregateName)) ?? string.Empty;
            Operation = info.GetString(nameof(Operation)) ?? string.Empty;
            CurrentState = info.GetString(nameof(CurrentState)) ?? string.Empty;
        }

      
    }
}