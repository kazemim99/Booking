// ========================================
// Booksy.Core.Domain/Exceptions/BusinessRuleViolationException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Rules;
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    [Serializable]
    public sealed class BusinessRuleViolationException : DomainException
    {
        public override ErrorCode ErrorCode { get; }
        public string RuleName { get; }

        public BusinessRuleViolationException(IBusinessRule brokenRule)
            : base(brokenRule.Message)
        {
            // Try to parse the error code string to enum, default to BUSINESS_RULE_VIOLATION
            ErrorCode = Enum.TryParse<ErrorCode>(brokenRule.ErrorCode, out var code)
                ? code
                : Errors.ErrorCode.BUSINESS_RULE_VIOLATION;
            RuleName = brokenRule.GetType().Name;

            ExtensionData = new Dictionary<string, object>
            {
                ["ruleName"] = RuleName,
                ["ruleType"] = brokenRule.GetType().FullName ?? string.Empty
            };
        }

        public BusinessRuleViolationException(string ruleName, string message, ErrorCode errorCode)
            : base(message)
        {
            RuleName = ruleName;
            ErrorCode = errorCode;

            ExtensionData = new Dictionary<string, object>
            {
                ["ruleName"] = RuleName
            };
        }

        private BusinessRuleViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            RuleName = info.GetString(nameof(RuleName)) ?? string.Empty;
            var errorCodeStr = info.GetString(nameof(ErrorCode)) ?? "BUSINESS_RULE_VIOLATION";
            ErrorCode = Enum.TryParse<ErrorCode>(errorCodeStr, out var code)
                ? code
                : Errors.ErrorCode.BUSINESS_RULE_VIOLATION;
        }


    }
}