using RockTracker.Api.Common;
using RockTracker.Api.Resources;
using RockTracker.Api.Services;

namespace RockTracker.Api.Middleware;

public sealed class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyValidator _validator;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(RequestDelegate next, ApiKeyValidator validator, ILogger<ApiKeyAuthMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _next = next;
        _validator = validator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = HttpContextHelpers.GetApiKeyFromHeaders(context);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Rejected request to {Path} due to missing API key.", context.Request.Path);
            await WriteUnauthorizedAsync(context, Messages.ApiKeyRequiredDetail);
            return;
        }

        if (!_validator.IsValid(apiKey))
        {
            _logger.LogWarning("Rejected request to {Path} due to invalid API key.", context.Request.Path);
            await WriteUnauthorizedAsync(context, Messages.ApiKeyRequiredDetail);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
    {
        var problem = HttpResponseHelpers.CreateProblemDetails(
            context.Request.Path,
            StatusCodes.Status401Unauthorized,
            Messages.UnauthorizedTitle,
            detail: detail);

        await HttpResponseHelpers.WriteProblemsAsync(
            context.Response,
            problem,
            context.RequestAborted)
            .ConfigureAwait(false);
    }
}
