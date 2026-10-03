using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RockTracker.Api.Common;

public static class HttpResponseHelpers
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string ProblemContentType = "application/problem+json";

    public static ProblemDetails CreateProblemDetails(
        PathString? instancePath,
        int statusCode,
        string title,
        IList<string>? errors = null,
        string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = instancePath?.Value
        };

        if (errors is { Count: > 0 })
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]>
            {
                ["general"] = errors.ToArray()
            };
        }

        return problem;
    }

    public static async Task WriteProblemsAsync(
        HttpResponse response,
        ProblemDetails problem,
        CancellationToken cancellationToken = default)
    {
        response.StatusCode = problem.Status ?? 500;
        await response.WriteAsJsonAsync(problem, CamelCaseOptions, contentType: ProblemContentType, cancellationToken)
            .ConfigureAwait(false);
    }

    public static IActionResult ToProblemResult(ProblemDetails problem) =>
        new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemContentType }
        };
}
