using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Models;
using RockTracker.Api.Services;
using RockTracker.Api.Tests.Fakes;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class RocksIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RocksIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ITypiCodeClient>(FakeTypiCodeClient.Found());
            });
        });
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleEmpty()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-bad-title";

        var req = new CreateRockRequest
        {
            Title = "",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var resp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", req);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var txt = await resp.Content.ReadAsStringAsync();
        Assert.True(txt.Contains("error") || txt.Contains("errors"), "Expected response body to contain 'error' or 'errors'. Body: " + txt);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDueDateInPast()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-bad-date";

        var req = new CreateRockRequest
        {
            Title = "Past",
            Category = RockCategory.Revenue.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var resp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", req);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var txt = await resp.Content.ReadAsStringAsync();
        Assert.True(txt.Contains("error") || txt.Contains("errors"), "Expected response body to contain 'error' or 'errors'. Body: " + txt);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryInvalid()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-bad-cat";

        var payload = new
        {
            title = "BadCat",
            category = "999",
            dueDate = DateTimeOffset.UtcNow.AddDays(2)
        };

        var resp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", payload);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var txt = await resp.Content.ReadAsStringAsync();
        Assert.True(txt.Contains("error") || txt.Contains("errors"), "Expected response body to contain 'error' or 'errors'. Body: " + txt);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMemberIdEmpty()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = Uri.EscapeDataString(" ");

        var req = new CreateRockRequest
        {
            Title = "NoMember",
            Category = RockCategory.Other.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(2)
        };

        var resp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", req);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var txt = await resp.Content.ReadAsStringAsync();
        Assert.True(txt.Contains("error") || txt.Contains("errors"), "Expected response body to contain 'error' or 'errors'. Body: " + txt);
    }

    [Fact]
    public async Task CreateThenGetThenUpdate_StatusFlow()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();

        var createReq = new CreateRockRequest
        {
            Title = "Integration Rock",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        var memberId = "int-member";

        var createResp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", createReq);
        createResp.EnsureSuccessStatusCode();
        createResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        createResp.Headers.Location.Should().NotBeNull();
        createResp.Headers.Location!.ToString().Should().Contain($"/members/{memberId}/rocks");

        var created = await createResp.Content.ReadFromJsonAsync<Rock>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
        created.Should().NotBeNull();
        created!.MemberId.Should().Be(memberId);

        var getResp = await client.GetAsync($"/members/{memberId}/rocks");
        getResp.EnsureSuccessStatusCode();
        getResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var list = await getResp.Content.ReadFromJsonAsync<Rock[]>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        list.Should().NotBeNull();
        list!.Length.Should().BeGreaterOrEqualTo(1);

        var updateReq = new UpdateRockStatusRequest { Status = RockStatus.Completed };
        var patchResp = await client.PatchAsJsonAsync($"/members/{memberId}/rocks/{created.Id}", updateReq);
        patchResp.EnsureSuccessStatusCode();
        patchResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updated = await patchResp.Content.ReadFromJsonAsync<Rock>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(RockStatus.Completed);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_WhenStatusMissing()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-patch-no-status";

        var createReq = new CreateRockRequest
        {
            Title = "Needs Status Update",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var createResp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<Rock>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        var resp = await client.PatchAsJsonAsync($"/members/{memberId}/rocks/{created!.Id}", new { });

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var txt = await resp.Content.ReadAsStringAsync();
        Assert.True(txt.Contains("error") || txt.Contains("errors"), "Expected response body to contain 'error' or 'errors'. Body: " + txt);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenRockMissing()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var updateReq = new UpdateRockStatusRequest { Status = RockStatus.Completed };

        var resp = await client.PatchAsJsonAsync($"/members/missing-member/rocks/{Guid.NewGuid()}", updateReq);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUnprocessableEntity_WhenInvalidTransition()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-invalid-transition";

        var createReq = new CreateRockRequest
        {
            Title = "Already Completed",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var createResp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<Rock>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        await client.PatchAsJsonAsync($"/members/{memberId}/rocks/{created!.Id}", new UpdateRockStatusRequest { Status = RockStatus.Completed });

        var resp = await client.PatchAsJsonAsync($"/members/{memberId}/rocks/{created.Id}", new UpdateRockStatusRequest { Status = RockStatus.Missed });

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenRequestContainsUnknownProperty()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-unknown-property";

        var payload = new
        {
            title = "Valid Title",
            category = RockCategory.Health.Name,
            dueDate = DateTimeOffset.UtcNow.AddDays(1),
            unexpectedField = "should not be accepted"
        };

        var resp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", payload);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_WhenRequestContainsUnknownProperty()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
            });
        });

        var client = factory.CreateAuthorizedClient();
        var memberId = "int-unknown-property-status";

        var createReq = new CreateRockRequest
        {
            Title = "For Unknown Property Update",
            Category = RockCategory.Health.Name,
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var createResp = await client.PostAsJsonAsync($"/members/{memberId}/rocks", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<Rock>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        var payload = new
        {
            status = nameof(RockStatus.Completed),
            unexpectedField = "should not be accepted"
        };

        var resp = await client.PatchAsJsonAsync($"/members/{memberId}/rocks/{created!.Id}", payload);

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
