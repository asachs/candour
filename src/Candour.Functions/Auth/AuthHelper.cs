namespace Candour.Functions.Auth;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

public static class AuthHelper
{
    public static bool ValidateApiKey(HttpRequestData req, IConfiguration configuration)
    {
        var configuredKey = configuration["Candour:ApiKey"];

        // If no API key configured, bypass auth (development mode)
        if (string.IsNullOrEmpty(configuredKey))
            return true;

        if (!req.Headers.TryGetValues("X-Api-Key", out var values))
            return false;

        var providedKey = values.FirstOrDefault() ?? string.Empty;

        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        if (configuredBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes);
    }
}
