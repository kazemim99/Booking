// ========================================
// Booksy.Core.Domain/Exceptions/DomainValidationException.cs
// ========================================
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when domain validation fails
    /// </summary>
    [Serializable]
    public sealed class DomainValidationException : DomainException
    {
        public override string ErrorCode => "DOMAIN_VALIDATION_FAILED";
        public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

        public DomainValidationException(string message, Dictionary<string, string[]> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors;

            ExtensionData = new Dictionary<string, object>
            {
                ["validationErrors"] = validationErrors
            };
        }

        public DomainValidationException(string propertyName, string errorMessage)
            : this(
                $"Validation failed for property '{propertyName}': {errorMessage}",
                new Dictionary<string, string[]> { [propertyName] = new[] { errorMessage } })
        {
        }

        public DomainValidationException(string propertyName, params string[] errorMessages)
            : this(
                $"Validation failed for property '{propertyName}'",
                new Dictionary<string, string[]> { [propertyName] = errorMessages })
        {
        }

        private DomainValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ValidationErrors = (Dictionary<string, string[]>?)info.GetValue(
                nameof(ValidationErrors),
                typeof(Dictionary<string, string[]>)) ?? new Dictionary<string, string[]>();
        }

    }
}