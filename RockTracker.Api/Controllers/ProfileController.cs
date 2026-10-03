using Microsoft.AspNetCore.Mvc;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Models;
using RockTracker.Api.Services;

namespace RockTracker.Api.Controllers;

[ApiController]
[Route("members/{memberId}/profile")]
public class ProfileController : ControllerBase
{
    private readonly ITypiCodeClient _typiCodeClient;
    private readonly IRockStore _rockStore;

    public ProfileController(ITypiCodeClient typiCodeClient, IRockStore rockStore)
    {
        _typiCodeClient = typiCodeClient ?? throw new ArgumentNullException(nameof(typiCodeClient));
        _rockStore = rockStore ?? throw new ArgumentNullException(nameof(rockStore));
    }

    [HttpGet("enriched")]
    public async Task<IActionResult> GetEnrichedProfile([FromRoute] string memberId, CancellationToken cancellationToken)
    {
        var result = await _typiCodeClient.GetEnrichedProfileAsync(memberId, cancellationToken);
        if (result.Status == EnrichedProfileFetchStatus.NotFound)
            return NotFound();

        var rocks = _rockStore.GetAll(memberId);
        return Ok(new EnrichedProfileWithRocks
        {
            Profile = result.Profile,
            Rocks = rocks,
            IsEnrichmentAvailable = result.Status == EnrichedProfileFetchStatus.Found
        });
    }
}
