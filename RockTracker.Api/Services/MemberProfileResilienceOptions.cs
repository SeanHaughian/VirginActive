namespace RockTracker.Api.Services;

/// <summary>
/// Configures the Polly resilience pipeline used for outbound member profile requests.
/// Bound from the "ExternalApis:TypiCode:Resilience" configuration section.
/// </summary>
public sealed class MemberProfileResilienceOptions
{
    public const string SectionName = "ExternalApis:TypiCode:Resilience";

    public int OuterTimeoutSeconds { get; set; } = 15;
    public int InnerTimeoutSeconds { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public double RetryBaseDelaySeconds { get; set; } = 1;
    public bool UseJitter { get; set; } = true;
}
