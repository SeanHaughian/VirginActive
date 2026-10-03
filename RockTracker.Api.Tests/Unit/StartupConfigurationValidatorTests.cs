using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RockTracker.Api.Startup;
using Xunit;

namespace RockTracker.Api.Tests.Unit;

public class StartupConfigurationValidatorTests
{
    private static IConfiguration CreateConfiguration(Dictionary<string, string?> data) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

    [Fact]
    public void ValidateRequired_DoesNotThrow_WhenAllRequiredValuesPresent()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ExternalApis:TypiCode:BaseUrl"] = "https://example.com/",
            ["Authentication:ApiKeyHash"] = "10000:salt:hash"
        });

        var exception = Record.Exception(() => StartupConfigurationValidator.ValidateRequired(configuration));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRequired_Throws_WhenBaseUrlMissing()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:ApiKeyHash"] = "10000:salt:hash"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateRequired(configuration));

        Assert.Contains("ExternalApis:TypiCode:BaseUrl", exception.Message);
    }

    [Fact]
    public void ValidateRequired_Throws_WhenNoApiKeyConfigured()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ExternalApis:TypiCode:BaseUrl"] = "https://example.com/"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateRequired(configuration));

        Assert.Contains("Authentication:ApiKeyHash", exception.Message);
    }

    [Fact]
    public void ValidateRequired_Throws_WhenOnlyPlaintextApiKeyPresent()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ExternalApis:TypiCode:BaseUrl"] = "https://example.com/",
            ["Authentication:ApiKey"] = "plaintext-key"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => StartupConfigurationValidator.ValidateRequired(configuration));

        Assert.Contains("Authentication:ApiKeyHash", exception.Message);
    }
}
