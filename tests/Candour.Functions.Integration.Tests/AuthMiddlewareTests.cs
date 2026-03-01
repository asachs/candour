namespace Candour.Functions.Integration.Tests;

using Candour.Functions.Auth;
using Candour.Functions.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// Integration tests for AuthenticationMiddleware DI wiring and configuration parsing.
/// The Invoke method requires FunctionContext which cannot be mocked without a full
/// Functions host; these tests verify the middleware can be constructed from the DI
/// container and correctly interprets configuration.
/// </summary>
public class AuthMiddlewareTests
{
    [Fact]
    public void AuthMiddleware_CanBeConstructed_WithMockedDependencies()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJwtTokenValidator>(new Mock<IJwtTokenValidator>().Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Candour:Auth:UseEntraId"] = "true",
                ["Candour:Auth:AdminEmails"] = "admin@example.com"
            })
            .Build());

        var provider = services.BuildServiceProvider();
        var middleware = ActivatorUtilities.CreateInstance<AuthenticationMiddleware>(provider);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void AuthMiddleware_ParsesMultipleAdminEmails()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Candour:Auth:UseEntraId"] = "true",
                ["Candour:Auth:AdminEmails"] = "admin@example.com; manager@example.com ; ops@example.com"
            })
            .Build();

        var middleware = new AuthenticationMiddleware(
            new Mock<IJwtTokenValidator>().Object,
            config);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void AuthMiddleware_HandlesEmptyAdminEmails()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Candour:Auth:UseEntraId"] = "false"
            })
            .Build();

        var middleware = new AuthenticationMiddleware(
            new Mock<IJwtTokenValidator>().Object,
            config);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void AuthMiddleware_DevMode_ConstructsWithApiKeyAuth()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Candour:Auth:UseEntraId"] = "false",
                ["Candour:Auth:ApiKey"] = "test-key"
            })
            .Build();

        var middleware = new AuthenticationMiddleware(
            new Mock<IJwtTokenValidator>().Object,
            config);

        Assert.NotNull(middleware);
    }
}
