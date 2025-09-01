// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Net;
using Booksy.SharedKernel.EventStore;
using Booksy.Core.Domain.Domain.Exceptions;
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;
using Booksy.Core.Domain.Infrastructure.Configuration;
using Booksy.Core.Domain.Infrastructure.Middleware;


// Booksy.API.Infrastructure/ExceptionHandling/Handlers/DomainExceptionHandler.cs
namespace Booksy.Core.Domain.Infrastructure.Middleware.Handlers
{
    public class DomainExceptionHandler : IExceptionHandler
    {
        public int Order => 1;

        public bool CanHandle(Exception exception)
        {
            return exception is DomainException;
        }

        public (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception, ApiResponseOptions options)
        {
            var domainException = (DomainException)exception;

            return domainException switch
            {
                EntityNotFoundException notFound => HandleEntityNotFound(notFound),
                DomainValidationException validation => HandleDomainValidation(validation, options),
                BusinessRuleViolationException businessRule => HandleBusinessRuleViolation(businessRule),
                ConcurrencyException concurrency => HandleConcurrency(concurrency),
                _ => HandleGenericDomainException(domainException)
            };
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleEntityNotFound(EntityNotFoundException exception)
        {
            return ((int)HttpStatusCode.NotFound, new ErrorResponse
            {
                Code = exception.ErrorCode,
                Message = exception.Message,
                Target = exception.EntityName,
                Extensions = new Dictionary<string, object>
                {
                    ["entityName"] = exception.EntityName,
                    ["entityId"] = exception.EntityId
                }
            });
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleDomainValidation(
            DomainValidationException exception,
            ApiResponseOptions options)
        {
            var validationErrors = exception.ValidationErrors
                .Take(options.MaxValidationErrors)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return ((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = exception.ErrorCode,
                Message = exception.Message,
                ValidationErrors = validationErrors,
                Extensions = validationErrors.Count > options.MaxValidationErrors
                    ? new Dictionary<string, object> { ["truncated"] = true }
                    : null
            });
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleBusinessRuleViolation(
            BusinessRuleViolationException exception)
        {
            return ((int)HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = exception.ErrorCode,
                Message = exception.Message,
                Target = exception.RuleName
            });
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleConcurrency(ConcurrencyException exception)
        {
            return ((int)HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = "CONCURRENCY_CONFLICT",
                Message = exception.Message,
                Extensions = new Dictionary<string, object>
                {
                    ["aggregateId"] = exception.AggregateId,
                    ["expectedVersion"] = exception.ExpectedVersion,
                    ["actualVersion"] = exception.ActualVersion
                }
            });
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleGenericDomainException(DomainException exception)
        {
            return ((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = exception.ErrorCode,
                Message = exception.Message
            });
        }
    }
}
