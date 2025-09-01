using MediatR;

namespace Booksy.Core.Domain.Application.Abstractions;

/// <summary>
/// Marker interface for commands (write operations)
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
/// Marker interface for commands with return values
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse> { }