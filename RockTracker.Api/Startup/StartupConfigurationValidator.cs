namespace RockTracker.Api.Startup;

public static class StartupConfigurationValidator
{
    private static readonly string[] RequiredKeys =
    [
        "ExternalApis:TypiCode:BaseUrl"
    ];

    public static void ValidateRequired(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var missingKeys = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToList();

        if (string.IsNullOrWhiteSpace(configuration["Authentication:ApiKeyHash"]))
        {
            missingKeys.Add("Authentication:ApiKeyHash");
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required configuration value(s): {string.Join(", ", missingKeys)}.");
        }
    }
}
