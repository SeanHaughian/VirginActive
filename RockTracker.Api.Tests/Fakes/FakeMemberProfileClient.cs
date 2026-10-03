using System.Threading;
using System.Threading.Tasks;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Clients.TypiCode.Models;

namespace RockTracker.Api.Tests.Fakes;

internal sealed class FakeTypiCodeClient : ITypiCodeClient
{
    private readonly EnrichedProfileFetchResult _result;

    public FakeTypiCodeClient(TypiCodeEnrichedProfile? profile)
    {
        _result = profile is null
            ? EnrichedProfileFetchResult.NotFound()
            : EnrichedProfileFetchResult.Found(profile);
    }

    private FakeTypiCodeClient(EnrichedProfileFetchResult result)
    {
        _result = result;
    }

    public static FakeTypiCodeClient Unavailable() => new(EnrichedProfileFetchResult.Unavailable());

    public static FakeTypiCodeClient Found(string memberId = "default-member") => new(new TypiCodeEnrichedProfile
    {
        MemberId = memberId,
        Id = 1,
        Name = "Default Test Member",
        Username = "default-test-member",
        Email = "default-test-member@example.com",
        Phone = "555-0100",
        Website = "example.com"
    });

    public Task<EnrichedProfileFetchResult> GetEnrichedProfileAsync(string memberId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_result);
    }
}
