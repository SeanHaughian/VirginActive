using RockTracker.Api.Common;
using RockTracker.Api.Resources;

namespace RockTracker.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteMemberNotFoundResponseAsync(context, ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Messages.UnhandledExceptionLog);
            await WriteExceptionResponseAsync(context).ConfigureAwait(false);
        }
    }

    private static async Task WriteMemberNotFoundResponseAsync(HttpContext context, KeyNotFoundException ex)
    {
        var problem = HttpResponseHelpers.CreateProblemDetails(
            context.Request.Path,
            StatusCodes.Status404NotFound,
            "Member Not Found",
            detail: ex.Message);

        await HttpResponseHelpers.WriteProblemsAsync(
            context.Response,
            problem,
            context.RequestAborted)
            .ConfigureAwait(false);
    }

    private static async Task WriteExceptionResponseAsync(HttpContext context)
    {
        var problem = HttpResponseHelpers.CreateProblemDetails(
            context.Request.Path,
            StatusCodes.Status500InternalServerError,
            Messages.UnexpectedErrorTitle);

        await HttpResponseHelpers.WriteProblemsAsync(
            context.Response,
            problem,
            context.RequestAborted)
            .ConfigureAwait(false);
    }
}
