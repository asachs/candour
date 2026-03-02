namespace Candour.Functions.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

public interface IJwtTokenValidator
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}

public class EntraIdJwtTokenValidator : IJwtTokenValidator
{
    private readonly EntraIdOptions _options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

    // Personal Microsoft accounts (MSA) issue tokens from a different tenant
    private const string MsaConsumerTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    public EntraIdJwtTokenValidator(IOptions<EntraIdOptions> options)
    {
        _options = options.Value;
        // Use 'common' metadata to get signing keys for both organizational and personal accounts
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var config = await _configManager.GetConfigurationAsync(CancellationToken.None);

        var audience = string.IsNullOrEmpty(_options.Audience) ? _options.ClientId : _options.Audience;
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuers = new[]
            {
                // v2.0 endpoint issuers
                $"https://login.microsoftonline.com/{_options.TenantId}/v2.0",
                $"https://login.microsoftonline.com/{MsaConsumerTenantId}/v2.0",
                // v1.0 endpoint issuers (sts.windows.net)
                $"https://sts.windows.net/{_options.TenantId}/",
                $"https://sts.windows.net/{MsaConsumerTenantId}/"
            },
            ValidAudiences = new[] { audience, _options.ClientId, $"api://{_options.ClientId}" },
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}

public class NoOpJwtTokenValidator : IJwtTokenValidator
{
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token) =>
        Task.FromResult<ClaimsPrincipal?>(null);
}
