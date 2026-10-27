namespace Reviews.WebApi.Middleware;

using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Помилка при виконанні запиту {Path}", httpContext.Request.Path);

        var (statusCode, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ресурс не знайдено"),
            ValidationException => (StatusCodes.Status400BadRequest, "Помилка валідації"),
            DomainException => (StatusCodes.Status400BadRequest, "Доменна помилка"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфлікт даних"),
            MongoWriteException mwe when mwe.WriteError.Category == ServerErrorCategory.DuplicateKey
                => (StatusCodes.Status409Conflict, "Дублікат запису"),
            MongoConnectionException => (StatusCodes.Status503ServiceUnavailable, "Сервіс бази даних недоступний"),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = ex.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }
}