// ========================================
// Booksy.Core.Application/Behaviors/IdempotencyBehavior.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Idempotency;
using Booksy.Core.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Core.Application.Behaviors
{
    /// <summary>
    /// Enforces at-most-once execution for commands marked <see cref="IRequireIdempotency"/> using an atomic
    /// database reservation (<see cref="IIdempotencyStore"/>), replacing the old non-atomic cache check-then-act.
    /// <para>
    /// Flow: reserve the key (an INSERT against a unique <c>(RequestType, Key)</c> — the serialization point). If we
    /// win, run the handler exactly once, then store its serialized result. If the key is already <b>Completed</b>,
    /// return the stored result (a retried request never re-charges). If it is still <b>in-flight</b> (a concurrent
    /// duplicate), throw <see cref="IdempotencyConflictException"/> → HTTP 409. On handler failure we release the
    /// reservation so the request can be retried; a crashed in-flight reservation is reclaimed after a staleness window.
    /// </para>
    /// </summary>
    public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

        public IdempotencyBehavior(
            IServiceProvider serviceProvider,
            ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only commands that opt in are protected.
            if (request is not IRequireIdempotency)
                return await next();

            var store = _serviceProvider.GetService<IIdempotencyStore>();
            if (store is null)
            {
                // No store registered for this context — do not silently drop protection on a money command.
                _logger.LogWarning(
                    "{CommandType} requires idempotency but no IIdempotencyStore is registered; proceeding WITHOUT protection.",
                    typeof(TRequest).Name);
                return await next();
            }

            var requestType = typeof(TRequest).Name;

            // A client-supplied key makes retries idempotent. If absent, generate one (still guards this attempt +
            // concurrent duplicates of it) and log — clients SHOULD send an Idempotency-Key header.
            var providedKey = ((IRequireIdempotency)request).IdempotencyKey;
            if (providedKey is null)
                _logger.LogWarning("{CommandType} has no idempotency key; generating one. Clients should send Idempotency-Key.", requestType);
            var key = (providedKey ?? Guid.NewGuid()).ToString("N");

            var outcome = await store.TryReserveAsync(requestType, key, StaleAfter, cancellationToken);

            switch (outcome.State)
            {
                case IdempotencyState.Completed:
                    _logger.LogInformation("Idempotent replay for {CommandType} key {Key}: returning stored result.", requestType, key);
                    return Deserialize(outcome.ResultJson);

                case IdempotencyState.InFlight:
                    _logger.LogWarning("Duplicate in-flight {CommandType} key {Key}: rejecting with 409.", requestType, key);
                    throw new IdempotencyConflictException(requestType, key);

                case IdempotencyState.Reserved:
                default:
                    break;
            }

            try
            {
                var response = await next();
                await store.CompleteAsync(requestType, key, JsonSerializer.Serialize(response), cancellationToken);
                return response;
            }
            catch
            {
                // Failed → release the reservation so the client can retry (the gateway was not left "reserved").
                await store.ReleaseAsync(requestType, key, CancellationToken.None);
                throw;
            }
        }

        private static TResponse Deserialize(string? json)
        {
            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException("Idempotency reservation completed without a stored result.");
            return JsonSerializer.Deserialize<TResponse>(json)!;
        }
    }
}
