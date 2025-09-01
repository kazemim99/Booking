// ========================================
// CQRS/CommandBus.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.CQRS;

/// <summary>
/// Command bus implementation using MediatR
/// </summary>
public sealed class CommandBus : ICommandBus
{
    private readonly IMediator _mediator;
    private readonly ILogger<CommandBus> _logger;

    public CommandBus(IMediator mediator, ILogger<CommandBus> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending command {CommandType}", command.GetType().Name);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogDebug("Command {CommandType} processed successfully", command.GetType().Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command {CommandType}", command.GetType().Name);
            throw;
        }
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending command {CommandType}", command.GetType().Name);

        try
        {
            await _mediator.Send(command, cancellationToken);

            _logger.LogDebug("Command {CommandType} processed successfully", command.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command {CommandType}", command.GetType().Name);
            throw;
        }
    }
}

