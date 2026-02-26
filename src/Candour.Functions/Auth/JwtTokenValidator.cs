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

    public EntraIdJwtTokenValidator(IOptions<EntraIdOptions> options)
    {
        _options = options.Value;
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _options.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var config = await _configManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://login.microsoftonline.com/{_options.TenantId}/v2.0",
            ValidAudience = string.IsNullOrEmpty(_options.Audience) ? _options.ClientId : _options.Audience,
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
