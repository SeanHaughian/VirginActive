using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RockTracker.Api.Tests.Integration;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateAuthorizedClient(this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "dev-local-api-key");
        return client;
    }
}
