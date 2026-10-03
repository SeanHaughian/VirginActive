namespace RockTracker.Api.Services;

public sealed class RockOperationResult
{
    public bool IsSuccess { get; }
    public Models.Rock? Rock { get; }
    public int ErrorStatusCode { get; }
    public string? ErrorTitle { get; }
    public IList<string>? Errors { get; }
    public string? Detail { get; }

    private RockOperationResult(bool isSuccess, Models.Rock? rock, int errorStatusCode, string? errorTitle, IList<string>? errors, string? detail)
    {
        IsSuccess = isSuccess;
        Rock = rock;
        ErrorStatusCode = errorStatusCode;
        ErrorTitle = errorTitle;
        Errors = errors;
        Detail = detail;
    }

    public static RockOperationResult Success(Models.Rock rock) => new(true, rock, 0, null, null, null);

    public static RockOperationResult Failure(int statusCode, string title, IList<string>? errors = null, string? detail = null) =>
        new(false, null, statusCode, title, errors, detail);
}
