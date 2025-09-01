using MediatR;

namespace Booksy.Core.Domain.Application.Abstractions;

/// <summary>
/// Marker interface for queries (read operations)
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse> { }