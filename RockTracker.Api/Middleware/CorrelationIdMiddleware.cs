using RockTracker.Api.Common;
using Serilog.Context;

namespace RockTracker.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrCreateCorrelationId(context);

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdConstants.HeaderName))
            {
                context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            }

            return Task.CompletedTask;
        });

        context.Items[CorrelationIdConstants.HeaderName] = correlationId;

        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private static string ExtractOrCreateCorrelationId(HttpContext context)
    {
        var headers = context.Request.Headers;
        return headers.ContainsKey(CorrelationIdConstants.HeaderName) &&
               !string.IsNullOrWhiteSpace(headers[CorrelationIdConstants.HeaderName])
            ? headers[CorrelationIdConstants.HeaderName].ToString()
            : Guid.NewGuid().ToString();
    }
}