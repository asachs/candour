namespace Candour.Functions.Integration.Tests;

using Candour.Functions.Middleware;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Integration tests for AnonymityMiddleware DI wiring.
/// The Invoke method requires FunctionContext/HttpRequestData which cannot
/// be mocked without a full Functions host; header-stripping route pattern
/// tests are in Candour.Functions.Tests.AnonymityMiddlewarePatternTests.
/// These tests verify the middleware can be instantiated from the DI container.
/// </summary>
public class AnonymityMiddlewareTests
{
    [Fact]
    public void AnonymityMiddleware_CanBeConstructed()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var middleware = ActivatorUtilities.CreateInstance<AnonymityMiddleware>(provider);

        Assert.NotNull(middleware);
    }
}
