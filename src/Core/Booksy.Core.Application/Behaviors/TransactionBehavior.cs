using MediatR;
using Microsoft.Extensions.Logging;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.Core.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for handling database transactions
    /// </summary>
    public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionBehavior(
            ILogger<TransactionBehavior<TRequest, TResponse>> logger,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestType = request.GetType();

            // Skip transactions for queries - they are read-only
            var isQuery = requestType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

            _logger.LogDebug("TransactionBehavior: {RequestName}, Type: {RequestType}, IsQuery: {IsQuery}",
                requestName, requestType.Name, isQuery);

            if (isQuery)
            {
                _logger.LogDebug("Skipping transaction for query: {RequestName}", requestName);
                return await next();
            }

            // If transaction is already active, don't create a new one
            if (_unitOfWork.HasActiveTransaction)
            {
                _logger.LogDebug("Transaction already active for {RequestName}", requestName);
                return await next();
            }

            _logger.LogDebug("Beginning transaction for {RequestName}", requestName);

            try
            {

                return await _unitOfWork.ExecuteInTransactionAsync(async () => await next(), cancellationToken);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                throw;
            }
        }
    }
}
