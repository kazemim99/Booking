// ========================================
// Booksy.Core.Domain/Abstractions/Rules/IBusinessRule.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.Rules
{
    /// <summary>
    /// Represents a business rule that must be satisfied
    /// </summary>
    public interface IBusinessRule
    {
        /// <summary>
        /// Gets the error message when the rule is broken
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the error code associated with this rule
        /// </summary>
        string ErrorCode { get; }

        /// <summary>
        /// Checks if the business rule is satisfied
        /// </summary>
        /// <returns>True if the rule is satisfied, false otherwise</returns>
        bool IsBroken();
    }
}