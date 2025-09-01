// ========================================
// Booksy.Core.Application/Abstractions/CQRS/ICommandHandler.cs
// ========================================
using MediatR;

namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Defines a handler for a command that doesn't return a value
    /// </summary>
    /// <typeparam name="TCommand">The type of command being handled</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Defines a handler for a command that returns a value
    /// </summary>
    /// <typeparam name="TCommand">The type of command being handled</typeparam>
    /// <typeparam name="TResponse">The type of response returned</typeparam>
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
    }
}