namespace RockTracker.Api.Common;

public static class HttpContextHelpers
{
    public static string BuildFullPath(HttpRequest request) =>
        request.Path + request.QueryString;

    public static string GetCorrelationId(HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdConstants.HeaderName, out var value)
            ? value?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

    public static string? GetApiKeyFromHeaders(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }

        return null;
    }

    public static string GetRequestDescription(HttpContext context)
    {
        var method = context.Request.Method;
        var path = BuildFullPath(context.Request);
        return $"{method} {path}";
    }
}

public static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";
}

public static class ApiKeyConstants
{
    public const string HeaderName = "X-Api-Key";
}
