// Booksy.API/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Booksy.API.Models;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Errors;
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
        response.ContentType = "application/problem+json"; // RFC 7807 media type

        ProblemDetailsResponse problemDetails;

        // Handle exceptions in order of specificity
        switch (exception)
        {
            case BusinessRuleViolationException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = ProblemDetailsResponse.Create(
                    businessEx.ErrorCode,
                    response.StatusCode,
                    businessEx.Message,
                    context.Request.Path,
                    businessEx.ExtensionData
                );
                break;

            case DomainValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationExtensions = new Dictionary<string, object>(validationEx.ExtensionData ?? new Dictionary<string, object>())
                {
                    ["validationErrors"] = validationEx.ValidationErrors
                };
                problemDetails = ProblemDetailsResponse.Create(
                    validationEx.ErrorCode,
                    response.StatusCode,
                    validationEx.Message,
                    context.Request.Path,
                    validationExtensions
                );
                break;

            case InvalidAggregateStateException aggregateEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = ProblemDetailsResponse.Create(
                    aggregateEx.ErrorCode,
                    response.StatusCode,
                    aggregateEx.Message,
                    context.Request.Path,
                    aggregateEx.ExtensionData
                );
                break;

            case DomainException domainEx:
                response.StatusCode = domainEx.HttpStatusCode;
                problemDetails = ProblemDetailsResponse.Create(
                    domainEx.ErrorCode,
                    response.StatusCode,
                    domainEx.Message,
                    context.Request.Path,
                    domainEx.ExtensionData
                );
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.NOT_FOUND,
                    response.StatusCode,
                    notFoundEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.UNAUTHORIZED,
                    response.StatusCode,
                    unauthorizedEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.FORBIDDEN,
                    response.StatusCode,
                    forbiddenEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.CONFLICT,
                    response.StatusCode,
                    conflictEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            case ExternalServiceException externalEx:
                response.StatusCode = externalEx.StatusCode ?? (int)HttpStatusCode.ServiceUnavailable;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.EXTERNAL_SERVICE_ERROR,
                    response.StatusCode,
                    externalEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            case ApplicationException appEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.VALIDATION_ERROR,
                    response.StatusCode,
                    appEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            // Handle ServiceCatalog ValidationException (custom) - using type name matching
            case Exception ex when ex.GetType().FullName == "Booksy.ServiceCatalog.Application.Exceptions.ValidationException":
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errorsProperty = ex.GetType().GetProperty("Errors");
                Dictionary<string, object>? validationExts = null;
                if (errorsProperty != null)
                {
                    var errors = errorsProperty.GetValue(ex) as Dictionary<string, List<string>>;
                    if (errors != null)
                    {
                        validationExts = new Dictionary<string, object>
                        {
                            ["validationErrors"] = errors.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.ToArray())
                        };
                    }
                }
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.VALIDATION_ERROR,
                    response.StatusCode,
                    "Validation failed",
                    context.Request.Path,
                    validationExts
                );
                break;

            case ValidationException fluentEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var fluentValidationExts = new Dictionary<string, object>
                {
                    ["validationErrors"] = fluentEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray())
                };
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.VALIDATION_ERROR,
                    response.StatusCode,
                    "Validation failed",
                    context.Request.Path,
                    fluentValidationExts
                );
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.UNAUTHORIZED,
                    response.StatusCode,
                    "Unauthorized",
                    context.Request.Path,
                    null
                );
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.NOT_FOUND,
                    response.StatusCode,
                    "Resource not found",
                    context.Request.Path,
                    null
                );
                break;

            case TaskCanceledException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.UNKNOWN_ERROR,
                    response.StatusCode,
                    "Request timeout",
                    context.Request.Path,
                    null
                );
                break;

            case OperationCanceledException:
                response.StatusCode = 499;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.UNKNOWN_ERROR,
                    response.StatusCode,
                    "Request was cancelled",
                    context.Request.Path,
                    null
                );
                break;

            case ArgumentNullException argNullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.VALIDATION_ERROR,
                    response.StatusCode,
                    $"Required parameter is missing: {argNullEx.ParamName}",
                    context.Request.Path,
                    new Dictionary<string, object> { ["paramName"] = argNullEx.ParamName ?? "unknown" }
                );
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = ProblemDetailsResponse.Create(
                    ErrorCode.VALIDATION_ERROR,
                    response.StatusCode,
                    argEx.Message,
                    context.Request.Path,
                    null
                );
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                if (_environment.IsDevelopment() || _environment.EnvironmentName == "Test")
                {
                    var debugExtensions = new Dictionary<string, object>
                    {
                        ["exceptionMessage"] = exception.Message ?? string.Empty,
                        ["exceptionType"] = exception.GetType().FullName ?? "Unknown",
                        ["stackTrace"] = exception.StackTrace ?? string.Empty
                    };
                    if (exception.InnerException != null)
                    {
                        debugExtensions["innerException"] = exception.InnerException.Message;
                    }
                    problemDetails = ProblemDetailsResponse.Create(
                        ErrorCode.INTERNAL_ERROR,
                        response.StatusCode,
                        exception.Message,
                        context.Request.Path,
                        debugExtensions
                    );
                }
                else
                {
                    _logger.LogError(exception, "An unhandled exception occurred");
                    problemDetails = ProblemDetailsResponse.Create(
                        ErrorCode.INTERNAL_ERROR,
                        response.StatusCode,
                        "An internal server error occurred. Please try again later.",
                        context.Request.Path,
                        null
                    );
                }
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment(),
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await response.WriteAsync(jsonResponse);
    }
}
