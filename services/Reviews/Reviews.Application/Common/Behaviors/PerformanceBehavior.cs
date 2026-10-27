namespace Reviews.Application.Common.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        if (timer.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("Повільний запит MediatR: {Name} ({Elapsed} ms)",
                typeof(TRequest).Name, timer.ElapsedMilliseconds);
        }

        return response;
    }
}