// ========================================
// Booksy.Core.Application/Abstractions/CQRS/IQueryHandler.cs
// ========================================
using MediatR;

namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Defines a handler for a query
    /// </summary>
    /// <typeparam name="TQuery">The type of query being handled</typeparam>
    /// <typeparam name="TResponse">The type of response returned</typeparam>
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
    }
}