using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Clients.TypiCode.Models;
using RockTracker.Api.Models;
using RockTracker.Api.Services;
using RockTracker.Api.Tests.Fakes;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class ProfileIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProfileIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEnrichedProfile_ReturnsOk_WithCompleteUserDataAndRocks_WhenMemberFound()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
        var enrichedProfile = new TypiCodeEnrichedProfile
        {
            MemberId = "1",
            Id = 1,
            Name = "Leanne Graham",
            Username = "Bret",
            Email = "Sincere@april.biz",
            Phone = "1-770-736-8031 x56442",
            Website = "hildegard.org",
            Address = new TypiCodeAddress
            {
                Street = "Kulas Light",
                Suite = "Apt. 556",
                City = "Gwenborough",
                Zipcode = "92998-3874",
                Geo = new TypiCodeGeo { Lat = "-37.3159", Lng = "81.1496" }
            },
            Company = new TypiCodeCompany
            {
                Name = "Romaguera-Crona",
                CatchPhrase = "Multi-layered client-server neural-net",
                Bs = "harness real-time e-markets"
            }
        };

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
                services.AddSingleton<ITypiCodeClient>(new FakeTypiCodeClient(enrichedProfile));
            });
        });

        var client = factory.CreateAuthorizedClient();

        var resp = await client.GetAsync("/members/1/profile/enriched");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<EnrichedProfileWithRocks>(new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.IsEnrichmentAvailable.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.MemberId.Should().Be("1");
        result.Profile.Name.Should().Be("Leanne Graham");
        result.Profile.Username.Should().Be("Bret");
        result.Profile.Email.Should().Be("Sincere@april.biz");
        result.Profile.Phone.Should().Be("1-770-736-8031 x56442");
        result.Profile.Website.Should().Be("hildegard.org");
        result.Profile.Address.Should().NotBeNull();
        result.Profile.Address!.Street.Should().Be("Kulas Light");
        result.Profile.Company.Should().NotBeNull();
        result.Profile.Company!.Name.Should().Be("Romaguera-Crona");
        result.Rocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEnrichedProfile_ReturnsNotFound_WhenExternalMemberNotFound()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
                services.AddSingleton<ITypiCodeClient>(new FakeTypiCodeClient(null));
            });
        });

        var client = factory.CreateAuthorizedClient();

        var resp = await client.GetAsync("/members/missing/profile/enriched");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEnrichedProfile_ReturnsOk_WithRocksAndUnavailableFlag_WhenExternalApiUnavailable()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
                services.AddSingleton<ITypiCodeClient>(FakeTypiCodeClient.Unavailable());
            });
        });

        var client = factory.CreateAuthorizedClient();

        var createReq = new { title = "Rock during outage", category = "Other", dueDate = DateTimeOffset.UtcNow.AddDays(3) };
        await client.PostAsJsonAsync("/members/unavailable-member/rocks", createReq);

        var resp = await client.GetAsync("/members/unavailable-member/profile/enriched");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var result = await resp.Content.ReadFromJsonAsync<EnrichedProfileWithRocks>(options);
        result.Should().NotBeNull();
        result!.IsEnrichmentAvailable.Should().BeFalse();
        result.Profile.Should().BeNull();
        result.Rocks.Should().ContainSingle(r => r.Title == "Rock during outage");
    }
}


