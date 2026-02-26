namespace Candour.Api.Auth;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = _configuration["Candour:ApiKey"];

        // If no API key configured, bypass auth (development mode)
        if (string.IsNullOrEmpty(configuredKey))
        {
            var bypassClaims = new[] { new Claim(ClaimTypes.Name, "dev-user"), new Claim(ClaimTypes.Role, "Admin") };
            var bypassIdentity = new ClaimsIdentity(bypassClaims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(bypassIdentity), Scheme.Name)));
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing API key"));

        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey.ToString());

        if (configuredBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "api-key-user"), new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
