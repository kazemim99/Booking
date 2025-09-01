// ========================================
// Booksy.Core.Application/Abstractions/Services/IApplicationService.cs
// ========================================
namespace Booksy.Core.Application.Abstractions.Services
{
    /// <summary>
    /// Marker interface for application services
    /// Application services orchestrate domain logic and infrastructure concerns
    /// </summary>
    public interface IApplicationService
    {
    }

    /// <summary>
    /// Base interface for application services with common functionality
    /// </summary>
    public interface IApplicationService<TContext> : IApplicationService
    {
        /// <summary>
        /// Gets the context for the application service
        /// </summary>
        TContext Context { get; }
    }
}