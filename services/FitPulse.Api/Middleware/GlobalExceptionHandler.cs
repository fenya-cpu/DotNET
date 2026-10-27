namespace FitPulse.Api.Middleware;

using FitPulse.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ноброблена помилка під час запиту: {Path}", httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Ресурс не знайдено"),
            DomainException => (StatusCodes.Status409Conflict, "Помилка бізнес-правила домену"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфлікт даних"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Помилка валідації"),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}