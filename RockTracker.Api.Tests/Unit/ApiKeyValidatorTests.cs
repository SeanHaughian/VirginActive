using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RockTracker.Api.Services;
using Xunit;

namespace RockTracker.Api.Tests.Unit;

public class ApiKeyValidatorTests
{
    private static ApiKeyValidator CreateValidator(string? apiKey = null, string? apiKeyHash = null)
    {
        var data = new Dictionary<string, string?>();
        if (apiKey is not null)
            data["Authentication:ApiKey"] = apiKey;
        if (apiKeyHash is not null)
            data["Authentication:ApiKeyHash"] = apiKeyHash;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        return new ApiKeyValidator(configuration, NullLogger<ApiKeyValidator>.Instance);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenOnlyPlaintextKeyConfigured()
    {
        var validator = CreateValidator(apiKey: "correct-key");

        Assert.False(validator.IsValid("correct-key"));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenNoKeyConfigured()
    {
        var validator = CreateValidator();

        Assert.False(validator.IsValid("anything"));
    }

    [Fact]
    public void HashKey_ThenIsValid_ReturnsTrue_ForCorrectKey()
    {
        var hash = ApiKeyValidator.HashKey("super-secret-key");
        var validator = CreateValidator(apiKeyHash: hash);

        Assert.True(validator.IsValid("super-secret-key"));
    }

    [Fact]
    public void HashKey_ThenIsValid_ReturnsFalse_ForIncorrectKey()
    {
        var hash = ApiKeyValidator.HashKey("super-secret-key");
        var validator = CreateValidator(apiKeyHash: hash);

        Assert.False(validator.IsValid("wrong-secret-key"));
    }

    [Theory]
    [InlineData("not-enough-parts")]
    [InlineData("abc:def")]
    [InlineData("notanumber:c2FsdA==:aGFzaA==")]
    [InlineData("10000:not-base64!!:aGFzaA==")]
    public void IsValid_ReturnsFalse_WhenStoredHashIsMalformed(string malformedHash)
    {
        var validator = CreateValidator(apiKeyHash: malformedHash);

        Assert.False(validator.IsValid("any-key"));
    }

    [Fact]
    public void Constructor_PrefersHashedKey_WhenBothPlaintextAndHashConfigured()
    {
        var hash = ApiKeyValidator.HashKey("hashed-key");
        var validator = CreateValidator(apiKey: "plaintext-key", apiKeyHash: hash);

        // The plaintext value is ignored entirely; only the hashed key is honored.
        Assert.False(validator.IsValid("plaintext-key"));
        Assert.True(validator.IsValid("hashed-key"));
    }

    [Fact]
    public void HashKey_ProducesDifferentOutput_ForSameInput_DueToRandomSalt()
    {
        var hash1 = ApiKeyValidator.HashKey("same-key");
        var hash2 = ApiKeyValidator.HashKey("same-key");

        Assert.NotEqual(hash1, hash2);
    }
}
