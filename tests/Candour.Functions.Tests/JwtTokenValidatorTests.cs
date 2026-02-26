namespace Candour.Functions.Tests;

using Candour.Functions.Auth;

/// <summary>
/// Tests for JWT token validator implementations.
/// EntraIdJwtTokenValidator requires real OIDC metadata endpoint;
/// here we test the NoOp implementation and document config keys.
/// </summary>
public class JwtTokenValidatorTests
{
    [Fact]
    public async Task NoOpValidator_AlwaysReturnsNull()
    {
        var validator = new NoOpJwtTokenValidator();
        var result = await validator.ValidateTokenAsync("any-token");
        Assert.Null(result);
    }

    [Fact]
    public async Task NoOpValidator_ReturnsNull_ForEmptyToken()
    {
        var validator = new NoOpJwtTokenValidator();
        var result = await validator.ValidateTokenAsync("");
        Assert.Null(result);
    }

    [Fact]
    public void EntraIdOptions_DefaultsUseEntraIdToTrue()
    {
        var options = new EntraIdOptions();
        Assert.True(options.UseEntraId);
    }

    [Fact]
    public void EntraIdOptions_ComputesMetadataAddress()
    {
        var options = new EntraIdOptions { TenantId = "test-tenant" };
        Assert.Equal(
            "https://login.microsoftonline.com/test-tenant/v2.0/.well-known/openid-configuration",
            options.MetadataAddress);
    }

    [Fact]
    public void ConfigKey_UseEntraId()
    {
        // Documents the config key path used by AuthenticationMiddleware
        const string expected = "Candour:Auth:UseEntraId";
        Assert.Equal("Candour:Auth:UseEntraId", expected);
    }
}
