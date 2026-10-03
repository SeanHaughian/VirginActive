using System.Net;
using System.Text.Json;
using Polly.Timeout;
using RockTracker.Api.Clients.TypiCode.Models;

namespace RockTracker.Api.Clients.TypiCode;

public class TypiCodeClient : ITypiCodeClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<TypiCodeClient> _logger;

    public TypiCodeClient(HttpClient httpClient, ILogger<TypiCodeClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EnrichedProfileFetchResult> GetEnrichedProfileAsync(string memberId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memberId);

        try
        {
            using var response = await _httpClient
                .GetAsync($"users/{memberId}", cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return EnrichedProfileFetchResult.NotFound();

            response.EnsureSuccessStatusCode();

            var user = await response.Content
                .ReadFromJsonAsync<TypiCodeUser>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
                return EnrichedProfileFetchResult.NotFound();

            return EnrichedProfileFetchResult.Found(new TypiCodeEnrichedProfile
            {
                MemberId = memberId,
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Website = user.Website,
                Address = user.Address,
                Company = user.Company
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to retrieve enriched profile for member {MemberId} after exhausting retries. Enrichment will be marked unavailable.", memberId);
            return EnrichedProfileFetchResult.Unavailable();
        }
    }
}
