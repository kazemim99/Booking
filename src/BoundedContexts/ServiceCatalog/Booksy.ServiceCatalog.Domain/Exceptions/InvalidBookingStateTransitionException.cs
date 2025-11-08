// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/InvalidBookingStateTransitionException.cs
// ========================================
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using System.Runtime.Serialization;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid booking state transition is attempted
    /// </summary>
    [Serializable]
    public class InvalidBookingStateTransitionException : DomainException
    {
        public BookingStatus CurrentState { get; }
        public string AttemptedTransition { get; }
        public override string ErrorCode => "INVALID_BOOKING_STATE_TRANSITION";

        public InvalidBookingStateTransitionException(
            BookingStatus currentState,
            string attemptedTransition)
            : base($"Cannot perform '{attemptedTransition}' operation from '{currentState}' state")
        {
            CurrentState = currentState;
            AttemptedTransition = attemptedTransition;
            ExtensionData = new Dictionary<string, object>
            {
                ["currentState"] = currentState.ToString(),
                ["attemptedTransition"] = attemptedTransition
            };
        }

        protected InvalidBookingStateTransitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CurrentState = (BookingStatus)(info.GetValue(nameof(CurrentState), typeof(BookingStatus)) ?? BookingStatus.Requested);
            AttemptedTransition = info.GetString(nameof(AttemptedTransition)) ?? "Unknown";
        }
    }
}
