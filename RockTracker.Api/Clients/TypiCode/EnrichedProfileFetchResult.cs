using RockTracker.Api.Clients.TypiCode.Models;

namespace RockTracker.Api.Clients.TypiCode;

public enum EnrichedProfileFetchStatus
{
    Found,
    NotFound,
    Unavailable
}

public record EnrichedProfileFetchResult
{
    public required EnrichedProfileFetchStatus Status { get; init; }
    public TypiCodeEnrichedProfile? Profile { get; init; }

    public static EnrichedProfileFetchResult Found(TypiCodeEnrichedProfile profile) =>
        new() { Status = EnrichedProfileFetchStatus.Found, Profile = profile };

    public static EnrichedProfileFetchResult NotFound() =>
        new() { Status = EnrichedProfileFetchStatus.NotFound };

    public static EnrichedProfileFetchResult Unavailable() =>
        new() { Status = EnrichedProfileFetchStatus.Unavailable };
}
