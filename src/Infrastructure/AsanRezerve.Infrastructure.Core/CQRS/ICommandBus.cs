// ========================================
// CQRS/ICommandBus.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.Infrastructure.Core.CQRS;


/// <summary>
/// Command bus abstraction
/// </summary>
public interface ICommandBus
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}

