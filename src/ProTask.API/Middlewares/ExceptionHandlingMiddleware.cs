using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProTask.Domain.Exceptions;

namespace ProTask.API.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                exception,
                "No se pudo escribir la respuesta de error porque la respuesta HTTP ya comenzó. TraceId: {TraceId}",
                context.TraceIdentifier);
            return;
        }

        var (statusCode, problemDetails) = MapException(context, exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Error no controlado al procesar {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Error controlado ({StatusCode}) en {Method} {Path}: {Message}. TraceId: {TraceId}",
                statusCode,
                context.Request.Method,
                context.Request.Path,
                exception.Message,
                context.TraceIdentifier);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = problemDetails is ValidationProblemDetails validationProblem
            ? JsonSerializer.Serialize(validationProblem, JsonOptions)
            : JsonSerializer.Serialize(problemDetails, JsonOptions);

        await context.Response.WriteAsync(payload);
    }

    private (int StatusCode, ProblemDetails Problem) MapException(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var instance = context.Request.Path.Value;

        switch (exception)
        {
            case ValidationException validationException:
            {
                var errors = validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "Error de validación",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Uno o más campos contienen valores inválidos.",
                    Instance = instance
                };
                problem.Extensions["traceId"] = traceId;
                return (StatusCodes.Status400BadRequest, problem);
            }

            case NotFoundException notFoundException:
            {
                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/404",
                    Title = "Recurso no encontrado",
                    Status = StatusCodes.Status404NotFound,
                    Detail = notFoundException.Message,
                    Instance = instance
                };
                problem.Extensions["errorCode"] = notFoundException.ErrorCode;
                problem.Extensions["traceId"] = traceId;
                return (StatusCodes.Status404NotFound, problem);
            }

            case BusinessRuleException businessRuleException:
            {
                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/409",
                    Title = "Conflicto con regla de negocio",
                    Status = StatusCodes.Status409Conflict,
                    Detail = businessRuleException.Message,
                    Instance = instance
                };
                problem.Extensions["errorCode"] = businessRuleException.ErrorCode;
                problem.Extensions["traceId"] = traceId;
                return (StatusCodes.Status409Conflict, problem);
            }

            case BadHttpRequestException:
            case JsonException:
            {
                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "Solicitud inválida",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "El cuerpo o los parámetros de la solicitud tienen un formato inválido.",
                    Instance = instance
                };
                problem.Extensions["traceId"] = traceId;
                return (StatusCodes.Status400BadRequest, problem);
            }

            default:
            {
                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/500",
                    Title = "Error interno del servidor",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "Ocurrió un error inesperado al procesar la solicitud.",
                    Instance = instance
                };
                problem.Extensions["traceId"] = traceId;
                return (StatusCodes.Status500InternalServerError, problem);
            }
        }
    }
}
