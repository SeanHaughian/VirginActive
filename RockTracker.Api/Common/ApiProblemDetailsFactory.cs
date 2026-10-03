using Microsoft.AspNetCore.Mvc;

namespace RockTracker.Api.Common;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(string? instancePath, int statusCode, string title, IList<string>? errors = null, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = instancePath
        };

        if (errors is { Count: > 0 })
            problem.Extensions["errors"] = new Dictionary<string, string[]> { ["general"] = errors.ToArray() };

        return problem;
    }

    public static IActionResult ToObjectResult(this ProblemDetails problem)
    {
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
