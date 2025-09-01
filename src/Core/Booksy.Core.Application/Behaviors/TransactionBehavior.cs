// ========================================
// Booksy.Core.Application/Behaviors/TransactionBehavior.cs
// ========================================
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
            // Only wrap commands in transactions, not queries
            var isCommand = request is ICommand ||
                 (request.GetType().GetInterfaces()
                     .Any(i => i.IsGenericType &&
                              i.GetGenericTypeDefinition() == typeof(ICommand<>)));

            if (!isCommand)
            {
                return await next();
            }

            if (_unitOfWork.HasActiveTransaction)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;

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

                //await _unitOfWork.BeginTransactionAsync(cancellationToken);


                //await _unitOfWork.CommitTransactionAsync(cancellationToken);

                //_logger.LogDebug("Transaction committed for {RequestName}", requestName);

                //return response;
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
