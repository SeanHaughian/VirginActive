using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Common;
using RockTracker.Api.Models;
using RockTracker.Api.Tests.Fakes;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class ApiKeyAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiKeyAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ITypiCodeClient>(FakeTypiCodeClient.Found());
            });
        });
    }

    [Theory]
    [InlineData("/members/any-member/rocks")]
    [InlineData("/members/any-member/profile/enriched")]
    public async Task Get_ReturnsUnauthorized_WhenApiKeyMissing(string path)
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(path);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/members/any-member/rocks")]
    [InlineData("/members/any-member/profile/enriched")]
    public async Task Get_ReturnsUnauthorized_WhenApiKeyIncorrect(string path)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong-key");

        var resp = await client.GetAsync(path);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRocks_ReturnsNotFound_WhenApiKeyCorrect_AndMemberHasNoRocks()
    {
        var client = _factory.CreateAuthorizedClient();

        var resp = await client.GetAsync("/members/any-member/rocks");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRock_ReturnsUnauthorized_WhenApiKeyMissing()
    {
        var client = _factory.CreateClient();

        var req = new CreateRockRequest
        {
            Title = "Unauthorized Attempt",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var resp = await client.PostAsJsonAsync("/members/any-member/rocks", req);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRock_ReturnsUnauthorized_WhenApiKeyIncorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong-key");

        var req = new CreateRockRequest
        {
            Title = "Unauthorized Attempt",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var resp = await client.PostAsJsonAsync("/members/any-member/rocks", req);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRock_ReturnsCreated_WhenApiKeyCorrect()
    {
        var client = _factory.CreateAuthorizedClient();

        var req = new CreateRockRequest
        {
            Title = "Authorized Rock Creation",
            Category = RockCategory.Other.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var resp = await client.PostAsJsonAsync("/members/authorized-member/rocks", req);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUnauthorized_WhenApiKeyMissing()
    {
        var client = _factory.CreateClient();

        var req = new UpdateRockStatusRequest { Status = RockStatus.Completed };
        var resp = await client.PatchAsJsonAsync($"/members/any-member/rocks/{Guid.NewGuid()}", req);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUnauthorized_WhenApiKeyIncorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong-key");

        var req = new UpdateRockStatusRequest { Status = RockStatus.Completed };
        var resp = await client.PatchAsJsonAsync($"/members/any-member/rocks/{Guid.NewGuid()}", req);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_DoesNotReturnUnauthorized_WhenApiKeyCorrect()
    {
        var client = _factory.CreateAuthorizedClient();

        var req = new UpdateRockStatusRequest { Status = RockStatus.Completed };
        var resp = await client.PatchAsJsonAsync($"/members/any-member/rocks/{Guid.NewGuid()}", req);

        // Rock won't exist, so this should fail validation/not-found rather than auth.
        resp.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRocks_ReturnsUnauthorized_WhenApiKeyHeaderIsWhitespace()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "   ");

        var resp = await client.GetAsync("/members/any-member/rocks");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRocks_Returns401ProblemDetails_WhenApiKeyMissing()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/members/any-member/rocks");

        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(401);
        problem.Title.Should().Be("Unauthorized");
        problem.Detail.Should().Be("A valid X-Api-Key header is required.");
        problem.Instance.Should().Be("/members/any-member/rocks");
    }

    [Fact]
    public async Task GetRocks_Returns401ProblemDetails_WhenApiKeyIncorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong-key");

        var resp = await client.GetAsync("/members/any-member/rocks");

        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(401);
        problem.Title.Should().Be("Unauthorized");
        problem.Detail.Should().Be("A valid X-Api-Key header is required.");
    }
}

