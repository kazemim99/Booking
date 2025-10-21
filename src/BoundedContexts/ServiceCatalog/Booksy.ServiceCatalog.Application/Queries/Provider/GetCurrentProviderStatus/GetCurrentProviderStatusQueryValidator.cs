// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetCurrentProviderStatus/GetCurrentProviderStatusQueryValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetCurrentProviderStatus
{
    /// <summary>
    /// Validator for GetCurrentProviderStatusQuery
    /// Minimal validation as query has no parameters
    /// </summary>
    public sealed class GetCurrentProviderStatusQueryValidator : AbstractValidator<GetCurrentProviderStatusQuery>
    {
        public GetCurrentProviderStatusQueryValidator()
        {
            // No validation rules needed - query uses authenticated user context
            // Validator exists for consistency with CQRS pattern
        }
    }
}
