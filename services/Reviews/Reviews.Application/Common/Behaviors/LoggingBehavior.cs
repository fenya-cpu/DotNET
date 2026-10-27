namespace Reviews.Application.Common.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("Обробка запиту {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Запит {RequestName} успішно оброблено", typeof(TRequest).Name);
        return response;
    }
}