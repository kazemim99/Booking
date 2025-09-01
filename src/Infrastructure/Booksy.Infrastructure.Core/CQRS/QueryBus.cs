// ========================================
// CQRS/QueryBus.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.CQRS;

/// <summary>
/// Query bus implementation using MediatR
/// </summary>
public sealed class QueryBus : IQueryBus
{
    private readonly IMediator _mediator;
    private readonly ILogger<QueryBus> _logger;

    public QueryBus(IMediator mediator, ILogger<QueryBus> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending query {QueryType}", query.GetType().Name);

        try
        {
            var result = await _mediator.Send(query, cancellationToken);

            _logger.LogDebug("Query {QueryType} processed successfully", query.GetType().Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query {QueryType}", query.GetType().Name);
            throw;
        }
    }
}

