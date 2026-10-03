namespace RockTracker.Api.Clients.TypiCode;

public interface ITypiCodeClient
{
    Task<EnrichedProfileFetchResult> GetEnrichedProfileAsync(string memberId, CancellationToken cancellationToken);
}
