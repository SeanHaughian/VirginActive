using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Services;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class MemberProfileResilienceIntegrationTests
{
    private static ServiceProvider BuildServiceProvider(
        SequencedHttpMessageHandler handler,
        out CapturingLoggerProvider loggerProvider)
    {
        var services = new ServiceCollection();
        var capturingProvider = new CapturingLoggerProvider();
        loggerProvider = capturingProvider;

        services.AddLogging(builder => builder.AddProvider(capturingProvider));

        services.AddHttpClient<ITypiCodeClient, TypiCodeClient>(client =>
            {
                client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddMemberProfileResilience();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetEnrichedProfileAsync_SucceedsAfterTransientFailures_WithinMaxRetries()
    {
        // Fails twice with a transient 503, then succeeds on the 3rd attempt.
        var handler = new SequencedHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("1")
            });

        using var provider = BuildServiceProvider(handler, out var loggerProvider);
        var client = provider.GetRequiredService<ITypiCodeClient>();

        var result = await client.GetEnrichedProfileAsync("1", CancellationToken.None);

        result.Status.Should().Be(EnrichedProfileFetchStatus.Found);
        handler.CallCount.Should().Be(3);
        loggerProvider.Entries.Should().HaveCount(2, "two retries were needed before success");
    }

    [Fact]
    public async Task GetEnrichedProfileAsync_ReturnsUnavailable_WhenFailuresExceedMaxRetries()
    {
        // Always fails with a transient 503; pipeline allows 3 retries (4 total attempts), all exhausted.
        var handler = new SequencedHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var provider = BuildServiceProvider(handler, out var loggerProvider);
        var client = provider.GetRequiredService<ITypiCodeClient>();

        var result = await client.GetEnrichedProfileAsync("1", CancellationToken.None);

        result.Status.Should().Be(EnrichedProfileFetchStatus.Unavailable);
        handler.CallCount.Should().Be(4, "the initial attempt plus 3 retries should all be exhausted");
        loggerProvider.Entries.Should().HaveCount(3, "exactly 3 retry attempts should have been logged");
    }

    [Fact]
    public async Task GetEnrichedProfileAsync_LogsRetryAttemptNumberAndReason_OnEachRetry()
    {
        var handler = new SequencedHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("1") });

        using var provider = BuildServiceProvider(handler, out var loggerProvider);
        var client = provider.GetRequiredService<ITypiCodeClient>();

        await client.GetEnrichedProfileAsync("1", CancellationToken.None);

        loggerProvider.Entries.Should().ContainSingle();
        var entry = loggerProvider.Entries[0];
        entry.Message.Should().Contain("Retry attempt 1");
        entry.Message.Should().Contain("ServiceUnavailable");
        entry.LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task GetEnrichedProfileAsync_ReturnsNotFound_WithoutRetrying_OnUpstream404()
    {
        var handler = new SequencedHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.NotFound));

        using var provider = BuildServiceProvider(handler, out var loggerProvider);
        var client = provider.GetRequiredService<ITypiCodeClient>();

        var result = await client.GetEnrichedProfileAsync("missing", CancellationToken.None);

        result.Status.Should().Be(EnrichedProfileFetchStatus.NotFound);
        handler.CallCount.Should().Be(1);
        loggerProvider.Entries.Should().BeEmpty();
    }

    private static System.Net.Http.Json.JsonContent JsonContent(string memberId) =>
        System.Net.Http.Json.JsonContent.Create(new
        {
            id = 1,
            name = "Test User",
            username = "testuser",
            email = "test@example.com",
            phone = "555-0100",
            website = "example.com"
        });
}

internal sealed class SequencedHttpMessageHandler : DelegatingHandler
{
    private readonly Func<HttpResponseMessage>[] _responses;
    private int _index;

    public SequencedHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
    {
        _responses = responses;
    }

    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var responseFactory = _index < _responses.Length ? _responses[_index] : _responses[^1];
        _index++;
        return Task.FromResult(responseFactory());
    }
}

internal sealed record LogEntry(LogLevel LogLevel, string Message);

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _provider;

        public CapturingLogger(CapturingLoggerProvider provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Warning)
                return;

            var message = formatter(state, exception);
            if (message.Contains("Retry attempt", StringComparison.Ordinal))
                _provider.Entries.Add(new LogEntry(logLevel, message));
        }
    }
}
