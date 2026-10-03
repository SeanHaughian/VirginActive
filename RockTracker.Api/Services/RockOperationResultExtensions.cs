using Microsoft.AspNetCore.Mvc;
using RockTracker.Api.Common;

namespace RockTracker.Api.Services;

public static class RockOperationResultExtensions
{
    public static IActionResult ToActionResult(this RockOperationResult result, PathString? requestPath)
    {
        var problem = HttpResponseHelpers.CreateProblemDetails(
            requestPath,
            result.ErrorStatusCode,
            result.ErrorTitle!,
            result.Errors,
            result.Detail);

        return HttpResponseHelpers.ToProblemResult(problem);
    }
}
