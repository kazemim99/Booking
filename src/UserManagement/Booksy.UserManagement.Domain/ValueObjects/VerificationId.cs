// ========================================
// Booksy.UserManagement.Domain/ValueObjects/VerificationId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for PhoneVerification aggregate
    /// </summary>
    public sealed class VerificationId : ValueObject
    {
        public Guid Value { get; }

        private VerificationId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("VerificationId cannot be empty", nameof(value));

            Value = value;
        }

        public static VerificationId From(Guid value) => new(value);

        public static VerificationId Create() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public static implicit operator Guid(VerificationId verificationId) => verificationId.Value;
    }
}
