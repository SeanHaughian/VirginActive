using RockTracker.Api.Clients.TypiCode.Models;

namespace RockTracker.Api.Models;

public record EnrichedProfileWithRocks
{
    public TypiCodeEnrichedProfile? Profile { get; init; }
    public required IEnumerable<Rock> Rocks { get; init; }
    public required bool IsEnrichmentAvailable { get; init; }
}
