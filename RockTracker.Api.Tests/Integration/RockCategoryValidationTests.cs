using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Services;
using RockTracker.Api.Tests.Fakes;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class RockCategoryValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RockCategoryValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(DateTimeOffset? clock = null)
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRockStore>(sp => new RockStore(tempFile, sp.GetRequiredService<ILogger<RockStore>>()));
                services.AddSingleton<ITypiCodeClient>(FakeTypiCodeClient.Found());
                if (clock.HasValue)
                    services.AddSingleton<IClock>(new TestClock(clock.Value));
            });
        });

        return factory.CreateAuthorizedClient();
    }

    [Theory]
    [InlineData("JustAfterQuarterStart", true)]
    [InlineData("QuarterEnd", true)]
    [InlineData("MidQuarter", true)]
    [InlineData("BeforeQuarterStart", false)]
    [InlineData("AfterQuarterEnd", false)]
    public async Task Revenue_DueDateQuarterBoundary_ReturnsExpectedStatus(string position, bool expectedSuccess)
    {
        // "Now" is pinned to the quarter start so every candidate due date below can also
        // satisfy the base "due date must be in the future" rule, isolating the category rule.
        var now = new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero);
        var quarterStart = QuarterCalculator.GetQuarterStart(now);
        var quarterEnd = QuarterCalculator.GetQuarterEnd(now);

        var dueDate = position switch
        {
            "JustAfterQuarterStart" => quarterStart.AddDays(1),
            "QuarterEnd" => quarterEnd,
            "MidQuarter" => quarterStart.AddDays(45),
            "BeforeQuarterStart" => quarterStart.AddDays(-1),
            "AfterQuarterEnd" => quarterEnd.AddTicks(1),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown boundary position")
        };

        var client = CreateClient(now);
        var payload = new { title = "Revenue test", category = "Revenue", dueDate };

        var resp = await client.PostAsJsonAsync($"/members/rev-{position}/rocks", payload);

        resp.StatusCode.Should().Be(expectedSuccess ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("ShortTitl", false)]
    [InlineData("ShortTitle", true)]
    [InlineData("ThisIsALongerHealthTitle", true)]
    public async Task Health_TitleLengthBoundary_ReturnsExpectedStatus(string title, bool expectedSuccess)
    {
        var client = CreateClient();
        var payload = new { title, category = "Health", dueDate = DateTimeOffset.UtcNow.AddDays(7) };

        var resp = await client.PostAsJsonAsync($"/members/health-{title.Length}/rocks", payload);

        resp.StatusCode.Should().Be(expectedSuccess ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Valid reason note", true)]
    public async Task Career_NoteRequirement_ReturnsExpectedStatus(string? note, bool expectedSuccess)
    {
        var client = CreateClient();
        var payload = new
        {
            title = "Career advancement plan",
            category = "Career",
            note,
            dueDate = DateTimeOffset.UtcNow.AddDays(14)
        };

        var resp = await client.PostAsJsonAsync("/members/career-note-test/rocks", payload);

        resp.StatusCode.Should().Be(expectedSuccess ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Other_NoAdditionalRules_AllowsCreation()
    {
        var client = CreateClient();
        var payload = new
        {
            title = "Misc task",
            category = "Other",
            dueDate = DateTimeOffset.UtcNow.AddDays(10)
        };

        var resp = await client.PostAsJsonAsync("/members/other-allowed/rocks", payload);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
