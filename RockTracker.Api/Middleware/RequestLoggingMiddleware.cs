using System.Diagnostics;
using RockTracker.Api.Common;

namespace RockTracker.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context).ConfigureAwait(false);
            LogResponse(context, stopwatch);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogException(context, stopwatch, ex);
            throw;
        }
    }

    private void LogResponse(HttpContext context, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        var statusCode = context.Response?.StatusCode ?? 0;
        var path = HttpContextHelpers.BuildFullPath(context.Request);
        var elapsed = stopwatch.Elapsed.TotalMilliseconds;

        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Debug
        };

        _logger.Log(logLevel, "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.000} ms",
            context.Request.Method, path, statusCode, elapsed);
    }

    private void LogException(HttpContext context, Stopwatch stopwatch, Exception ex)
    {
        stopwatch.Stop();
        var path = HttpContextHelpers.BuildFullPath(context.Request);
        var elapsed = stopwatch.Elapsed.TotalMilliseconds;

        _logger.LogError(ex, "HTTP {Method} {Path} threw an unhandled exception after {Elapsed:0.000} ms",
            context.Request.Method, path, elapsed);
    }
}
