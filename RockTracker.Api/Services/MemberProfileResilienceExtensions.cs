using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using RockTracker.Api.Clients.TypiCode;

namespace RockTracker.Api.Services;

public static class MemberProfileResilienceExtensions
{
    public static void AddMemberProfileResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("member-profile-resilience", (pipeline, context) =>
        {
            var options = context.ServiceProvider
                .GetRequiredService<IOptions<MemberProfileResilienceOptions>>().Value;

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.OuterTimeoutSeconds));

            pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = options.UseJitter,
                Delay = TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => IsTransientFailure(response.StatusCode)),
                OnRetry = args =>
                {
                    var logger = context.ServiceProvider.GetRequiredService<ILogger<TypiCodeClient>>();
                    logger.LogWarning(
                        "Retry attempt {AttemptNumber} for enriched profile request after delay of {DelayMs}ms. Reason: {Reason}",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    return default;
                }
            });

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.InnerTimeoutSeconds));
        });
    }

    private static bool IsTransientFailure(System.Net.HttpStatusCode statusCode) =>
        statusCode == System.Net.HttpStatusCode.RequestTimeout ||
        statusCode == System.Net.HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;
}
