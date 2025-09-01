// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Net;
using Booksy.SharedKernel.Infrastructure.Middleware.Responses;
using FluentValidation;
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;
using Booksy.Core.Domain.Infrastructure.Configuration;
using Booksy.Core.Domain.Infrastructure.Middleware;


// Booksy.API.Infrastructure/ExceptionHandling/Handlers/ValidationExceptionHandler.cs
namespace Booksy.Core.Domain.Infrastructure.Middleware.Handlers
{
    public class ValidationExceptionHandler : IExceptionHandler
    {
        public int Order => 2;

        public bool CanHandle(Exception exception)
        {
            return exception is ValidationException;
        }

        public (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception, ApiResponseOptions options)
        {
            var validationException = (ValidationException)exception;

            var groupedErrors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .Take(options.MaxValidationErrors)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "One or more validation errors occurred",
                ValidationErrors = groupedErrors,
                Details = validationException.Errors
                    .Take(options.MaxValidationErrors)
                    .Select(e => new ErrorDetail
                    {
                        Code = e.ErrorCode ?? "FIELD_VALIDATION_ERROR",
                        Message = e.ErrorMessage,
                        Target = e.PropertyName
                    })
                    .ToList()
            });
        }
    }
}
