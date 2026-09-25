// ========================================
// CQRS/IQueryBus.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.Infrastructure.Core.CQRS;



/// <summary>
/// Query bus abstraction
/// </summary>
public interface IQueryBus
{
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

