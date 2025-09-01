// ========================================
// CQRS/IQueryBus.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.Infrastructure.Core.CQRS;



/// <summary>
/// Query bus abstraction
/// </summary>
public interface IQueryBus
{
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

