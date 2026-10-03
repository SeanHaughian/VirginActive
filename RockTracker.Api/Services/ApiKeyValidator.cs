using System.Security.Cryptography;

namespace RockTracker.Api.Services;

public class ApiKeyValidator
{
    private const int PbkdfIterations = 10000;
    private const int HashLengthBytes = 32;

    private readonly string? _hashedKey;
    private readonly ILogger<ApiKeyValidator> _logger;

    public ApiKeyValidator(IConfiguration configuration, ILogger<ApiKeyValidator> logger)
    {
        _logger = logger;

        var hashedKey = configuration["Authentication:ApiKeyHash"];
        if (!string.IsNullOrWhiteSpace(hashedKey))
        {
            _hashedKey = hashedKey;
            _logger.LogInformation("API key loaded from Authentication:ApiKeyHash (hashed)");
            return;
        }

        _logger.LogWarning("No API key configured in Authentication:ApiKeyHash. All requests will be rejected.");
    }

    public bool IsValid(string providedKey)
    {
        if (string.IsNullOrWhiteSpace(_hashedKey) || string.IsNullOrWhiteSpace(providedKey))
        {
            return false;
        }

        try
        {
            return ConstantTimeHashComparison(providedKey, _hashedKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during API key validation");
            return false;
        }
    }

    private static bool ConstantTimeHashComparison(string plaintextKey, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var storedKeyHash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                plaintextKey,
                salt,
                iterations,
                HashAlgorithmName.SHA256);

            var computedHash = pbkdf2.GetBytes(HashLengthBytes);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedKeyHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string HashKey(string plaintextKey)
    {
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(
            plaintextKey,
            salt,
            PbkdfIterations,
            HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(HashLengthBytes);

        return $"{PbkdfIterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
