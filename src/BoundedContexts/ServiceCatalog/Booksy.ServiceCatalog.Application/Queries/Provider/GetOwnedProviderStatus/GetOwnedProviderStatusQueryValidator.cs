// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetOwnedProviderStatus/GetOwnedProviderStatusQueryValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus
{
    /// <summary>
    /// Validator for GetOwnedProviderStatusQuery
    /// Minimal validation as query has no parameters
    /// </summary>
    public sealed class GetOwnedProviderStatusQueryValidator : AbstractValidator<GetOwnedProviderStatusQuery>
    {
        public GetOwnedProviderStatusQueryValidator()
        {
            // No validation rules needed - query uses authenticated user context
            // Validator exists for consistency with CQRS pattern
        }
    }
}
