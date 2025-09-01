using MediatR;

namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Marker interface for commands that don't return a value
    /// </summary>
    public interface ICommand : IRequest
    {
        /// <summary>
        /// Gets the unique identifier for command idempotency
        /// </summary>
        Guid? IdempotencyKey { get; }
    }

    /// <summary>
    /// Marker interface for commands that return a value
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
        /// <summary>
        /// Gets the unique identifier for command idempotency
        /// </summary>
        Guid? IdempotencyKey { get; }
    }
}
