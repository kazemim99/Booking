// ========================================
// Booksy.ServiceCatalog.Domain/Exceptions/InvalidPaymentStateTransitionException.cs
// ========================================
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using System.Runtime.Serialization;

namespace Booksy.ServiceCatalog.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid payment state transition is attempted
    /// </summary>
    [Serializable]
    public class InvalidPaymentStateTransitionException : DomainException
    {
        public PaymentStatus CurrentState { get; }
        public string AttemptedTransition { get; }
        public override string ErrorCode => "INVALID_PAYMENT_STATE_TRANSITION";

        public InvalidPaymentStateTransitionException(
            PaymentStatus currentState,
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

        protected InvalidPaymentStateTransitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CurrentState = (PaymentStatus)(info.GetValue(nameof(CurrentState), typeof(PaymentStatus)) ?? PaymentStatus.Pending);
            AttemptedTransition = info.GetString(nameof(AttemptedTransition)) ?? "Unknown";
        }
    }
}
