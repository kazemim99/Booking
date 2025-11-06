// Booksy.API/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationException = Booksy.Core.Application.Exceptions.ApplicationException;

namespace Booksy.API.Middleware;

public partial class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ApiErrorResult("An error occurred while processing your request");

        // Handle exceptions in order of specificity
        switch (exception)
        {
            case BusinessRuleViolationException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult(businessEx.Message, businessEx.ErrorCode)
                {
                    Errors = businessEx.ExtensionData?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new[] { kvp.Value?.ToString() ?? string.Empty })
                };
                break;

            case DomainValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult(validationEx.Message, validationEx.ErrorCode)
                {
                    Errors = (Dictionary<string, string[]>)validationEx.ValidationErrors
                };
                break;

            case InvalidAggregateStateException aggregateEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult(aggregateEx.Message, aggregateEx.ErrorCode)
                {
                    Errors = aggregateEx.ExtensionData?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new[] { kvp.Value?.ToString() ?? string.Empty })
                };
                break;

            case DomainException domainEx:
                response.StatusCode = domainEx.HttpStatusCode;
                errorResponse = new ApiErrorResult(domainEx.Message, domainEx.ErrorCode);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = new ApiErrorResult(notFoundEx.Message, notFoundEx.ErrorCode);
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = new ApiErrorResult(unauthorizedEx.Message, unauthorizedEx.ErrorCode);
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse = new ApiErrorResult(forbiddenEx.Message, forbiddenEx.ErrorCode);
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse = new ApiErrorResult(conflictEx.Message, conflictEx.ErrorCode);
                break;

            case ExternalServiceException externalEx:
                response.StatusCode = externalEx.StatusCode ?? (int)HttpStatusCode.ServiceUnavailable;
                errorResponse = new ApiErrorResult(externalEx.Message, externalEx.ErrorCode);
                break;

            case ApplicationException appEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult(appEx.Message, appEx.ErrorCode);
                break;

            // Handle ServiceCatalog ValidationException (custom) - using type name matching
            case Exception ex when ex.GetType().FullName == "Booksy.ServiceCatalog.Application.Exceptions.ValidationException":
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errorsProperty = ex.GetType().GetProperty("Errors");
                if (errorsProperty != null)
                {
                    var errors = errorsProperty.GetValue(ex) as Dictionary<string, List<string>>;
                    errorResponse = new ApiErrorResult("Validation failed", "VALIDATION_ERROR")
                    {
                        Errors = errors?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToArray())
                    };
                }
                break;

            case ValidationException fluentEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult("Validation failed", "VALIDATION_ERROR")
                {
                    Errors = fluentEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray())
                };
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = new ApiErrorResult("Unauthorized", "UNAUTHORIZED");
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = new ApiErrorResult("Resource not found", "NOT_FOUND");
                break;

            case TaskCanceledException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse = new ApiErrorResult("Request timeout", "TIMEOUT");
                break;

            case OperationCanceledException:
                response.StatusCode = 499;
                errorResponse = new ApiErrorResult("Request was cancelled", "CANCELLED");
                break;

            case ArgumentNullException argNullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult($"Required parameter is missing: {argNullEx.ParamName}", "MISSING_PARAMETER");
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ApiErrorResult(argEx.Message, "INVALID_ARGUMENT");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                if (_environment.IsDevelopment() || _environment.EnvironmentName == "Test")
                {
                    errorResponse = new ApiErrorResult(exception.Message, "INTERNAL_ERROR")
                    {
                        Errors = new Dictionary<string, string[]>
                        {
                            ["message"] = new[] { exception.Message ?? string.Empty },
                            ["innerMessage"] = new[] { exception.InnerException != null ? exception.InnerException.Message : string.Empty}
                        }
                    };
                }
                else
                {

                    _logger.LogError(exception, "An unhandled exception occurred");

                    errorResponse = new ApiErrorResult(
                        "An internal server error occurred. Please try again later.",
                        "INTERNAL_ERROR");
                }
                break;
        }

        // ✅ Wrap error in same format as success responses
        var wrappedErrorResponse = new
        {
            success = false,
            statusCode = response.StatusCode.ToString(),  // ✅ Convert to string
            message = errorResponse.Message,
            data = (object?)null,
            error = new
            {
                code = errorResponse.Code,
                message = errorResponse.Message,
                errors = errorResponse.Errors
            },
            metadata = new
            {
                requestId = context.TraceIdentifier,
                timestamp = DateTimeOffset.UtcNow,
                path = context.Request.Path.Value,
                method = context.Request.Method
            }
        };

        var jsonResponse = JsonSerializer.Serialize(wrappedErrorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await response.WriteAsync(jsonResponse);
    }

    // Keep existing ApiErrorResult class
    public class ApiErrorResult
    {
        public string Message { get; set; }
        public string? Code { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }

        public ApiErrorResult(string message, string? code = null)
        {
            Message = message;
            Code = code;
        }
    }
}