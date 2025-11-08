// ========================================
// Booksy.Core.Domain/Exceptions/BusinessRuleViolationException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Rules;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    [Serializable]
    public sealed class BusinessRuleViolationException : DomainException
    {
        public override string ErrorCode { get; }
        public string RuleName { get; }

        public BusinessRuleViolationException(IBusinessRule brokenRule)
            : base(brokenRule.Message)
        {
            ErrorCode = brokenRule.ErrorCode;
            RuleName = brokenRule.GetType().Name;

            ExtensionData = new Dictionary<string, object>
            {
                ["ruleName"] = RuleName,
                ["ruleType"] = brokenRule.GetType().FullName ?? string.Empty
            };
        }

        public BusinessRuleViolationException(string ruleName, string message, string errorCode)
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
            ErrorCode = info.GetString(nameof(ErrorCode)) ?? "BUSINESS_RULE_VIOLATION";
        }

       
    }
}